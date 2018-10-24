using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.API.ModuleExtensions;
using Fabric.Authorization.API.Modules;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Resolvers.Models;
using Fabric.Authorization.Domain.Resolvers.Permissions;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Domain.Stores;
using IdentityModel;
using Nancy;
using Nancy.Extensions;
using Nancy.Responses;
using Serilog;

namespace Fabric.Authorization.API.Services
{
    public class AccessService
    {
        private readonly IPermissionResolverService _permissionResolverService;
        private readonly UserService _userService;
        private readonly IGroupStore _groupStore;
        private readonly ILogger _logger;

        public AccessService(IPermissionResolverService permissionResolverService, UserService userService, IGroupStore groupStore, ILogger logger)
        {
            _permissionResolverService = permissionResolverService ?? throw new ArgumentNullException(nameof(permissionResolverService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _groupStore = groupStore ?? throw new ArgumentNullException(nameof(groupStore));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task CheckUserAccess<T>(string grain, string securableItemName, FabricModule<T> module, params Predicate<Claim>[] requiredClaims)
        {
            var permissions = (await GetPermissions(module, grain, securableItemName)).ToList();
            module.RequiresPermissionsAndClaims<T>(module.SubjectId, permissions, grain, securableItemName, requiredClaims);
        }

        public async Task CheckAppAccess<T>(string clientId, string grain, string securableItem, ClientService clientService, FabricModule<T> module, params Predicate<Claim>[] requiredClaims)
        {
            var doesClientOwnItem = false;
            try
            {
                doesClientOwnItem =
                    await clientService.DoesClientOwnItem(clientId, grain, securableItem);
            }
            catch (NotFoundException<SecurableItem>)
            {
                module.AddBeforeHookOrExecute((context) => CreateFailureResponse<T>($"The securableItem: {securableItem} does not exist.",
                    context,
                    HttpStatusCode.BadRequest));
            }
            catch (NotFoundException<Client>)
            {
                doesClientOwnItem = false;
            }

            module.RequiresOwnershipAndClaims<T>(doesClientOwnItem, grain, securableItem, requiredClaims);
        }
        
        public async Task<IEnumerable<string>> GetGroupsForAuthenticatedUser(string subjectId, string providerId, ClaimsPrincipal currentUser)
        {
            var groupClaims = currentUser?.Claims
                .Where(c => c.Type == Claims.Roles || c.Type == Claims.Groups)
                .Distinct(new ClaimComparer())
                .Select(c => c.Value.ToString());

            var customAndChildGroups = new List<string>();
            try
            {
                // retrieve all custom groups the user is associated with plus their child AD groups
                customAndChildGroups = (await _userService.GetGroupsForUser(subjectId, providerId, true)).Select(g => g.Name).ToList();
            }
            // the user may not always be in the DB but we still need to calculate permissions based on the AD groups
            // in the access token
            catch (NotFoundException<User>)
            {
                _logger.Information($"User {subjectId} not found while attempting to retrieve groups.");
            }

            // retrieve all AD groups that are on the access token, ignoring the ones that we do not have registered in our DB
            var directoryGroups = (await _groupStore.Get(groupClaims, true)).ToList();

            // extract all the parent custom groups from the list of AD groups
            var flattenedDirectoryGroups = directoryGroups.Union(directoryGroups.SelectMany(g => g.Parents)).Select(g => g.Name);

            // combine the lists and remove any duplicates
            var allGroups = customAndChildGroups
                .Concat(flattenedDirectoryGroups)
                .Distinct()
                .ToList();

            _logger.Information($"found groups for user: {allGroups.ToString(",")}");

            return allGroups;
        }

        private async Task<IEnumerable<string>> GetPermissions<T>(FabricModule<T> module, string grain, string securableItemName)
        {
            var permissionResolutionResult = await _permissionResolverService.Resolve(new PermissionResolutionRequest
            {
                SubjectId = module.SubjectId,
                IdentityProvider = module.IdentityProvider,
                Grain = grain,
                SecurableItem = securableItemName,
                UserGroups = await GetGroupsForAuthenticatedUser(module.SubjectId, module.IdentityProvider, module.Context.CurrentUser)
            });
            var permissions = permissionResolutionResult.AllowedPermissions
                .Except(permissionResolutionResult.DeniedPermissions)
                .Select(p => p.ToString());
            return permissions;
        }

        public static JsonResponse CreateFailureResponse<T>(string message, NancyContext context, HttpStatusCode statusCode)
        {
            var error = ErrorFactory.CreateError<T>(message, statusCode);
            return new JsonResponse(error, new DefaultJsonSerializer(context.Environment),
                    context.Environment)
                { StatusCode = statusCode };
        }

    }
}
