using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Fabric.Authorization.API.ModuleExtensions;
using Fabric.Authorization.API.Modules;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Resolvers.Models;
using Fabric.Authorization.Domain.Resolvers.Permissions;
using Fabric.Authorization.Domain.Services;
using IdentityModel;
using Nancy;
using Serilog;

namespace Fabric.Authorization.API.Services
{
    public class AccessService
    {
        private readonly IPermissionResolverService _permissionResolverService;
        private readonly UserService _userService;
        private readonly ILogger _logger;
        public AccessService(IPermissionResolverService permissionResolverService, UserService userService, ILogger logger)
        {
            _permissionResolverService = permissionResolverService ?? throw new ArgumentNullException(nameof(permissionResolverService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task CheckSharedAccess<T>(Grain grain, string securableItemName, bool isWriteOperation, FabricModule<T> module)
        {
            var securableItemIsValid = IsSecurableItemChildOfGrain(grain, securableItemName);
            var permissionResolutionResult = await _permissionResolverService.Resolve(new PermissionResolutionRequest
            {
                SubjectId = module.SubjectId,
                IdentityProvider = module.IdentityProvider,
                Grain = grain.Name,
                SecurableItem = securableItemName,
                UserGroups = await GetGroupsForAuthenticatedUser(module.SubjectId, module.IdentityProvider, module.Context.CurrentUser)
            });
            var permissions = permissionResolutionResult.AllowedPermissions
                .Except(permissionResolutionResult.DeniedPermissions)
                .Select(p => p.ToString());
            module.RequiresSharedAccess<T>(grain, securableItemName, securableItemIsValid, isWriteOperation, permissions);
        }

        public async Task CheckAppAccess<T>(string clientId, string grain, string securableItem, ClientService clientService, FabricModule<T> module, params Predicate<Claim>[] requiredClaims)
        {
            var doesClientOwnItem = false;
            try
            {
                doesClientOwnItem =
                    await clientService.DoesClientOwnItem(clientId, grain, securableItem);
            }
            catch (NotFoundException<Client>)
            {
                doesClientOwnItem = false;
            }

            module.RequiresOwnershipAndClaims<T>(doesClientOwnItem, grain, securableItem, requiredClaims);
        }

        private bool IsSecurableItemChildOfGrain(Grain grain, string securableItemName)
        {
            foreach (var securableItem in grain.SecurableItems)
            {
                if (HasRequestedSecurableItem(securableItem, securableItemName))
                {
                    return true;
                }
            }
            return false;
        }

        private bool HasRequestedSecurableItem(SecurableItem parentSecurableItem, string securableItem)
        {
            if (parentSecurableItem.Name == securableItem)
            {
                return true;
            }
            var childSecurableItems = parentSecurableItem.SecurableItems;
            if (childSecurableItems == null || childSecurableItems.Count == 0)
            {
                return false;
            }

            if (childSecurableItems.Any(si => si.Name == securableItem))
            {
                return true;
            }

            return childSecurableItems.Any(
                childSecurableItem => HasRequestedSecurableItem(childSecurableItem, securableItem));
        }

        private async Task<IEnumerable<string>> GetGroupsForAuthenticatedUser(string subjectId, string providerId, ClaimsPrincipal currentUser)
        {
            var userClaims = currentUser?.Claims
                .Where(c => c.Type == "role" || c.Type == "groups")
                .Distinct(new ClaimComparer())
                .Select(c => c.Value.ToString());

            var groups = new List<string>();
            try
            {
                groups = (await _userService.GetGroupsForUser(subjectId, providerId)).ToList();
            }
            catch (NotFoundException<User>)
            {
                _logger.Information($"User {subjectId} not found while attempting to retrieve groups.");
            }

            var allClaims = userClaims?
                .Concat(groups)
                .Distinct();

            _logger.Information($"found claims for user: {allClaims.ToString(",")}");

            return allClaims ?? new string[] { };
        }

    }
}
