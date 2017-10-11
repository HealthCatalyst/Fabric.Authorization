using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores.Services;
using Fabric.Authorization.Domain.Validators;
using IdentityModel;
using Microsoft.AspNetCore.Http;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Serilog;
using System.Text;

namespace Fabric.Authorization.API.Modules
{
    public class UsersModule : FabricModule<User>
    {
        private readonly ClientService _clientService;
        private readonly PermissionService _permissionService;
        private readonly UserService _userService;

        public UsersModule(
            ClientService clientService,
            PermissionService permissionService,
            UserService userService,
            UserValidator validator,
            ILogger logger) : base("/v1/user", logger, validator)
        {
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
            _clientService = clientService ?? throw new ArgumentNullException(nameof(clientService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));

            // Get all the permissions for a user
            Get("/permissions", async _ => await GetCurrentUserPermissions().ConfigureAwait(false), null,
                "GetUserPermissions");           

            Post("/{identityProvider}/{subjectId}/permissions",
                async param => await this.AddGranularPermissions(param).ConfigureAwait(false), null,
                "AddGranularPermissions");

            Delete("/{identityProvider}/{subjectId}/permissions",
                async param => await this.DeleteGranularPermissions(param).ConfigureAwait(false), null,
                "DeleteGranularPermissions");

            Get("/{identityProvider}/{subjectId}/groups", async _ => await GetUserGroups().ConfigureAwait(false), null, "GetUserGroups");
        }       

        private async Task<dynamic> GetUserGroups()
        {
            this.RequiresClaims(AuthorizationReadClaim);
            var groupUserRequest = this.Bind<GroupUserRequest>();
            var groups = await _userService.GetGroupsForUser(groupUserRequest.SubjectId, groupUserRequest.IdentityProvider);
            return groups;
        }

        private async Task<dynamic> AddGranularPermissions(dynamic param)
        {
            var permissions = this.Bind<List<PermissionApiModel>>();

            if (permissions.Count == 0)
                return CreateFailureResponse(
                    "No permissions specified to add, ensure an array of permissions is included in the request.",
                    HttpStatusCode.BadRequest);

            foreach (var perm in permissions)
                await CheckAccess(_clientService, perm.Grain, perm.SecurableItem, AuthorizationManageClientsClaim);

            var granularPermission = new GranularPermission
            {
                Id = $"{param.subjectId}:{param.identityProvider}",
                DeniedPermissions = permissions
                    .Where(p => p.PermissionAction == PermissionAction.Deny)
                    .Select(p => p.ToPermissionDomainModel()),
                AdditionalPermissions = permissions
                    .Where(p => p.PermissionAction == PermissionAction.Allow)
                    .Select(p => p.ToPermissionDomainModel())
            };

            await _permissionService.AddUserGranularPermissions(granularPermission);
            return HttpStatusCode.NoContent;
        }      

        private async Task<dynamic> DeleteGranularPermissions(dynamic param)
        {
            var permissions = this.Bind<List<PermissionApiModel>>();

            if (permissions.Count == 0)
                return CreateFailureResponse(
                    "No permissions specified to add, ensure an array of permissions is included in the request.",
                    HttpStatusCode.BadRequest);

            foreach (var perm in permissions)
                await CheckAccess(_clientService, perm.Grain, perm.SecurableItem, AuthorizationManageClientsClaim);

            var granularPermission = new GranularPermission
            {
                Id = $"{param.subjectId}:{param.identityProvider}",
                DeniedPermissions = permissions
                    .Where(p => p.PermissionAction == PermissionAction.Deny)
                    .Select(p => p.ToPermissionDomainModel()),
                AdditionalPermissions = permissions
                    .Where(p => p.PermissionAction == PermissionAction.Allow)
                    .Select(p => p.ToPermissionDomainModel())
            };

            try
            {
                await _permissionService.DeleteGranularPermissions(granularPermission);
                return HttpStatusCode.NoContent;
            }            
            catch(InvalidPermissionException ex)
            {
                var invalidPermissions = new StringBuilder();
                foreach (DictionaryEntry item in ex.Data)
                {
                    invalidPermissions.Append($"{item.Key}: {item.Value}. ");
                }

                return CreateFailureResponse(
                    $"{ex.Message} {invalidPermissions}", 
                    HttpStatusCode.BadRequest);
            }
        }     

        private async Task<dynamic> GetCurrentUserPermissions()
        {
            var userPermissionRequest = this.Bind<UserInfoRequest>();
            await SetDefaultRequest(userPermissionRequest);
            await CheckAccess(_clientService, userPermissionRequest.Grain, userPermissionRequest.SecurableItem,
                AuthorizationReadClaim);

            var subjectId = SubjectId;
            var identityProvider = IdentityProvider;
            var groups = await GetGroupsForAuthenticatedUser(subjectId, identityProvider).ConfigureAwait(false);

            var permissions = await _permissionService.GetPermissionsForUser(
                $"{subjectId}:{identityProvider}",
                groups,
                userPermissionRequest.Grain,
                userPermissionRequest.SecurableItem);

            return new UserPermissionsApiModel
            {
                RequestedGrain = userPermissionRequest.Grain,
                RequestedSecurableItem = userPermissionRequest.SecurableItem,
                Permissions = permissions
            };
        }

        private async Task<string[]> GetGroupsForAuthenticatedUser(string subjectId, string providerId)
        {
            var userClaims = Context.CurrentUser?.Claims
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
                Logger.Information($"User {subjectId} not found while attempting to retrieve groups.");
            }

            var allClaims = userClaims?
                .Concat(groups)
                .Distinct()
                .ToList();

            Logger.Information($"found claims for user: {string.Join(",", allClaims)}");

            return allClaims == null
                ? new string[] { }
                : allClaims.ToArray();
        }

        private async Task SetDefaultRequest(UserInfoRequest request)
        {
            if (string.IsNullOrEmpty(request.Grain) && string.IsNullOrEmpty(request.SecurableItem))
            {
                var client = await _clientService.GetClient(ClientId);
                request.Grain = TopLevelGrains.AppGrain;
                request.SecurableItem = client.TopLevelSecurableItem.Name;
            }
        }
       
    }
}