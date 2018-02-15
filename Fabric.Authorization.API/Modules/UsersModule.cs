using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.API.Services;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Resolvers.Models;
using Fabric.Authorization.Domain.Resolvers.Permissions;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Domain.Validators;
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
        private readonly GrainService _grainService;
        private readonly IPermissionResolverService _permissionResolverService;

        public UsersModule(
            ClientService clientService,
            PermissionService permissionService,
            UserService userService,
            GrainService grainService,
            IPermissionResolverService permissionResolverService,
            UserValidator validator,
            AccessService accessService,
            ILogger logger) : base("/v1/user", logger, validator, accessService)
        {
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
            _clientService = clientService ?? throw new ArgumentNullException(nameof(clientService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _grainService = grainService ?? throw new ArgumentNullException(nameof(grainService));
            _permissionResolverService = permissionResolverService ?? throw new ArgumentNullException(nameof(permissionResolverService));

            // Get all the permissions for a user
            Get("/permissions",
                async _ => await GetCurrentUserPermissions().ConfigureAwait(false), null,
                "GetCurrentUserPermissions");

            Post("/", 
                async _ => await this.AddUser().ConfigureAwait(false), null, 
                "AddUser");

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

            Get("/{identityProvider/{subjectId}/roles",
                async _ => await GetUserRoles().ConfigureAwait(false), null,
                "GetUserRoles");

            Post("/{identityProvider}/{subjectId}/roles",
                async param => await AddRolesToUser(param).ConfigureAwait(false), null,
                "AddRolesToUser");
        }

        private async Task<dynamic> AddUser()
        {
            this.RequiresClaims(AuthorizationWriteClaim);
            var user = this.Bind<UserApiModel>().ToUserDomainModel();
            Validate(user);
            var userModel = await _userService.AddUser(user);
            return CreateSuccessfulPostResponse($"{userModel.IdentityProvider}/{userModel.SubjectId}", userModel.ToUserApiModel());
        }

        private async Task<dynamic> GetUserPermissions(dynamic param)
        {
            var userPermissionRequest = this.Bind<UserInfoRequest>();
            var isGrainEmpty = string.IsNullOrEmpty(userPermissionRequest.Grain);
            await SetDefaultRequest(userPermissionRequest);
            CheckReadAccess();

            var permissionResolutionResult = await _permissionResolverService.Resolve(new PermissionResolutionRequest
            {
                SubjectId = param.subjectId,
                IdentityProvider = param.identityProvider,
                Grain = userPermissionRequest.Grain,
                SecurableItem = userPermissionRequest.SecurableItem,
                IncludeSharedPermissions = isGrainEmpty,
                UserGroups = await _userService.GetGroupsForUser(param.subjectId, param.identityProvider)
            });

            return permissionResolutionResult.AllowedPermissions
                .Concat(permissionResolutionResult.DeniedPermissions)
                .Select(p => p.ToResolvedPermissionApiModel());
        }

        private async Task<dynamic> GetCurrentUserPermissions()
        {
            var userPermissionRequest = this.Bind<UserInfoRequest>();
            var isGrainEmpty = string.IsNullOrEmpty(userPermissionRequest.Grain);
            await SetDefaultRequest(userPermissionRequest);
            CheckReadAccess();

            var permissionResolutionResult = await _permissionResolverService.Resolve(new PermissionResolutionRequest
            {
                SubjectId = SubjectId,
                IdentityProvider = IdentityProvider,
                Grain = userPermissionRequest.Grain,
                SecurableItem = userPermissionRequest.SecurableItem,
                IncludeSharedPermissions = isGrainEmpty,
                UserGroups = await AccessService.GetGroupsForAuthenticatedUser(SubjectId, IdentityProvider, Context.CurrentUser)
            });

            var permissionRequestContexts = permissionResolutionResult.AllowedPermissions.Select(
                p => new PermissionRequestContext
                {
                    RequestedGrain = p.Grain,
                    RequestedSecurableItem = p.SecurableItem
                }).Distinct(new PermissionRequestContextComparer());

            return new UserPermissionsApiModel
            {
                PermissionRequestContexts = permissionRequestContexts,
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
                await CheckWriteAccess(_clientService, _grainService, perm.Grain, perm.SecurableItem);
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
                await CheckWriteAccess(_clientService, _grainService, perm.Grain, perm.SecurableItem);
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

        private async Task<dynamic> GetUserRoles()
        {
            this.RequiresClaims(AuthorizationReadClaim);
            var roleUserRequest = this.Bind<RoleUserRequest>();
            try
            {
                var roles =
                    await _userService.GetRolesForUser(roleUserRequest.SubjectId, roleUserRequest.IdentityProvider);
                return roles.Select(r => r.ToRoleApiModel());
            }
            catch (NotFoundException<User>)
            {
                return CreateFailureResponse(
                    $"User with SubjectId: {roleUserRequest.SubjectId} and Identity Provider: {roleUserRequest.IdentityProvider} was not found",
                    HttpStatusCode.NotFound);
            }
        }

        private async Task<dynamic> AddRolesToUser(dynamic param)
        {
            var apiRoles = this.Bind<List<RoleApiModel>>();
            foreach (var roleApiModel in apiRoles)
            {
                await CheckWriteAccess(_clientService, _grainService, roleApiModel.Grain, roleApiModel.SecurableItem);
            }
            var domainRoles = apiRoles.Select(r => r.ToRoleDomainModel()).ToList();
            try
            {
                User user = await _userService.AddRolesToUser(domainRoles, param.subjectId.ToString(),
                    param.identityProvider.ToString());
                return CreateSuccessfulPostResponse($"{user.IdentityProvider}/{user.SubjectId}", user,
                    HttpStatusCode.OK);
            }
            catch (NotFoundException<User>)
            {
                return CreateFailureResponse(
                    $"User with SubjectId: {param.subjectId} and Identity Provider: {param.identityProvider} was not found",
                    HttpStatusCode.NotFound);
            }
            catch (AggregateException e)
            {
                return CreateFailureResponse(e, HttpStatusCode.BadRequest);
            }
        }
        
        private async Task SetDefaultRequest(UserInfoRequest request)
        {
            if (string.IsNullOrEmpty(request.Grain))
            {
                request.Grain = TopLevelGrains.AppGrain;
            }

            if (string.IsNullOrEmpty(request.SecurableItem))
            {
                var client = await _clientService.GetClient(ClientId);
                request.SecurableItem = client.TopLevelSecurableItem.Name;
            }
        }
    }
}