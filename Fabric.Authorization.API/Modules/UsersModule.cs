using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Resolvers.Models;
using Fabric.Authorization.Domain.Resolvers.Permissions;
using Fabric.Authorization.Domain.Stores.Services;
using Fabric.Authorization.Domain.Validators;
using IdentityModel;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Serilog;

namespace Fabric.Authorization.API.Modules
{
    public class UsersModule : FabricModule<User>
    {
        private readonly ClientService _clientService;
        private readonly PermissionService _permissionService;
        private readonly UserService _userService;
        private readonly IPermissionResolverService _permissionResolverService;

        public UsersModule(
            ClientService clientService,
            PermissionService permissionService,
            UserService userService,
            RoleService roleService,
            IPermissionResolverService permissionResolverService,
            UserValidator validator,
            ILogger logger) : base("/v1/user", logger, validator)
        {
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
            _clientService = clientService ?? throw new ArgumentNullException(nameof(clientService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _permissionResolverService = permissionResolverService ?? throw new ArgumentNullException(nameof(permissionResolverService));

            // Get all the permissions for a user
            Get("/permissions",
                async _ => await GetCurrentUserPermissions().ConfigureAwait(false), null,
                "GetCurrentUserPermissions");

            Get("/{identityProvider}/{subjectId}/permissions",
                async param => await this.GetUserPermissions(param).ConfigureAwait(false), null,
                "GetUserPermissions");

            Post("/{identityProvider}/{subjectId}/permissions",
                async param => await this.AddGranularPermissions(param).ConfigureAwait(false), null,
                "AddGranularPermissions");

            Delete("/{identityProvider}/{subjectId}/permissions",
                async param => await this.DeleteGranularPermissions(param).ConfigureAwait(false), null,
                "DeleteGranularPermissions");

            Get("/{identityProvider}/{subjectId}/groups",
                async _ => await GetUserGroups().ConfigureAwait(false), null,
                "GetUserGroups");
        }

        private async Task<dynamic> GetUserPermissions(dynamic param)
        {
            var userPermissionRequest = this.Bind<UserInfoRequest>();
            await SetDefaultRequest(userPermissionRequest);
            await CheckAccess(
                _clientService,
                userPermissionRequest.Grain,
                userPermissionRequest.SecurableItem,
                AuthorizationReadClaim);

            var permissionResolutionResult = await _permissionResolverService.Resolve(new PermissionResolutionRequest
            {
                SubjectId = param.subjectId,
                IdentityProvider = param.identityProvider,
                Grain = userPermissionRequest.Grain,
                SecurableItem = userPermissionRequest.SecurableItem,
                UserGroups = await _userService.GetGroupsForUser(param.subjectId, param.identityProvider)
            });

            return permissionResolutionResult.AllowedPermissions
                .Concat(permissionResolutionResult.DeniedPermissions)
                .Select(p => p.ToPermissionApiModel());
        }

        private async Task<dynamic> GetCurrentUserPermissions()
        {
            var userPermissionRequest = this.Bind<UserInfoRequest>();
            await SetDefaultRequest(userPermissionRequest);
            await CheckAccess(_clientService, userPermissionRequest.Grain, userPermissionRequest.SecurableItem,
                AuthorizationReadClaim);

            var permissionResolutionResult = await _permissionResolverService.Resolve(new PermissionResolutionRequest
            {
                SubjectId = SubjectId,
                IdentityProvider = IdentityProvider,
                Grain = userPermissionRequest.Grain,
                SecurableItem = userPermissionRequest.SecurableItem,
                UserGroups = await GetGroupsForAuthenticatedUser(SubjectId, IdentityProvider)
            });

            return new UserPermissionsApiModel
            {
                RequestedGrain = userPermissionRequest.Grain,
                RequestedSecurableItem = userPermissionRequest.SecurableItem,
                Permissions = permissionResolutionResult.AllowedPermissions
                    .Except(permissionResolutionResult.DeniedPermissions)
                    .Select(p => p.ToString())
            };
        }

        private async Task<dynamic> AddGranularPermissions(dynamic param)
        {
            var permissions = this.Bind<List<PermissionApiModel>>();

            if (permissions.Count == 0)
            {
                return CreateFailureResponse(
                    "No permissions specified to add, ensure an array of permissions is included in the request.",
                    HttpStatusCode.BadRequest);
            }

            var requestErrors = new List<string>();

            var permissionsWithMissingIds = permissions.Where(p => !p.Id.HasValue).ToList();
            var permissionsWithInvalidActions = permissions.Where(p => p.PermissionAction != PermissionAction.Allow
                                                                       && p.PermissionAction != PermissionAction.Deny)
                .ToList();

            if (permissionsWithMissingIds.Any())
            {
                requestErrors.AddRange(permissionsWithMissingIds.Select(p => $"{p.Name} is missing its id property."));
            }

            if (permissionsWithInvalidActions.Any())
            {
                requestErrors.AddRange(permissionsWithInvalidActions.Select(p => $"{p.Name} {p.Id} does not have a valid permissionAction."));
            }

            if (requestErrors.Any())
            {
                return CreateFailureResponse(requestErrors, HttpStatusCode.BadRequest);
            }

            foreach (var perm in permissions)
            {
                await CheckAccess(_clientService, perm.Grain, perm.SecurableItem, AuthorizationManageClientsClaim);
            }

            var allowedPermissions = permissions
                .Where(p => p.PermissionAction == PermissionAction.Allow)
                .Select(p => p.ToPermissionDomainModel());

            var deniedPermissions = permissions
                .Where(p => p.PermissionAction == PermissionAction.Deny)
                .Select(p => p.ToPermissionDomainModel());

            var granularPermission = new GranularPermission
            {
                Id = $"{param.subjectId}:{param.identityProvider}",
                AdditionalPermissions = allowedPermissions,
                DeniedPermissions = deniedPermissions
            };

            try
            {
                await _permissionService.AddUserGranularPermissions(granularPermission);
                return HttpStatusCode.NoContent;
            }
            catch (InvalidPermissionException ex)
            {
                var invalidPermissions = new StringBuilder();
                foreach (DictionaryEntry item in ex.Data)
                {
                    invalidPermissions.Append($"{item.Key}: {item.Value}. ");
                }

                return CreateFailureResponse(
                    $"{ex.Message} {invalidPermissions}",
                    HttpStatusCode.Conflict);
            }
        }

        private async Task<dynamic> DeleteGranularPermissions(dynamic param)
        {
            var permissions = this.Bind<List<PermissionApiModel>>();

            if (permissions.Count == 0)
            {
                return CreateFailureResponse(
                    "No permissions specified to add, ensure an array of permissions is included in the request.",
                    HttpStatusCode.BadRequest);
            }

            foreach (var perm in permissions)
            {
                await CheckAccess(_clientService, perm.Grain, perm.SecurableItem, AuthorizationManageClientsClaim);
            }

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
            catch (InvalidPermissionException ex)
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

        private async Task<dynamic> GetUserGroups()
        {
            this.RequiresClaims(AuthorizationReadClaim);
            var groupUserRequest = this.Bind<GroupUserRequest>();
            try
            {
               
                var groups =
                    await _userService.GetGroupsForUser(groupUserRequest.SubjectId, groupUserRequest.IdentityProvider);
                return groups;
            }
            catch (NotFoundException<User>)
            {
                return CreateFailureResponse(
                    $"User with SubjectId: {groupUserRequest.SubjectId} and Identity Provider: {groupUserRequest.IdentityProvider} was not found",
                    HttpStatusCode.NotFound);
            }
        }

        private async Task<IEnumerable<string>> GetGroupsForAuthenticatedUser(string subjectId, string providerId)
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
                .Distinct();

            Logger.Information($"found claims for user: {allClaims.ToString(",")}");

            return allClaims ?? new string[] { };
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