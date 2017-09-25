using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
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

            Post("/{userId}/AdditionalPermissions",
                async param => await this.AddGranularPermissions(param, denied: false).ConfigureAwait(false), null,
                "AddPermissions");

            Post("/{userId}/DeniedPermissions",
                async param => await this.AddGranularPermissions(param, denied: true).ConfigureAwait(false), null,
                "AddDeniedPermissions");

            Get("/{subjectId}/groups", async _ => await GetUserGroups().ConfigureAwait(false), null, "GetUserGroups");
        }

        private async Task<dynamic> GetUserGroups()
        {
            this.RequiresClaims(AuthorizationReadClaim);
            var groupUserRequest = this.Bind<GroupUserRequest>();
            var groups = await _userService.GetGroupsForUser(groupUserRequest.SubjectId);
            return groups;
        }

        private async Task<dynamic> AddGranularPermissions(dynamic param, bool denied)
        {
            var apiModel = this.Bind<GranularPermissionApiModel>();

            if (apiModel.Target != param["userId"])
                return CreateFailureResponse("Target must be the user id.", HttpStatusCode.BadRequest);

            foreach (var perm in apiModel.Permissions)
                await CheckAccess(_clientService, perm.Grain, perm.SecurableItem, AuthorizationManageClientsClaim);

            var granularPermissions = apiModel.ToGranularPermissionDomainModel();

            if (denied)
                granularPermissions.DeniedPermissions = apiModel.Permissions.Select(p => p.ToPermissionDomainModel());
            else
                granularPermissions.AdditionalPermissions =
                    apiModel.Permissions.Select(p => p.ToPermissionDomainModel());

            await _permissionService.AddUserGranularPermissions(granularPermissions);
            return HttpStatusCode.NoContent;
        }

        private async Task<dynamic> GetCurrentUserPermissions()
        {
            var userPermissionRequest = this.Bind<UserInfoRequest>();
            await SetDefaultRequest(userPermissionRequest);
            await CheckAccess(_clientService, userPermissionRequest.Grain, userPermissionRequest.SecurableItem,
                AuthorizationReadClaim);

            var subjectId = Context.CurrentUser.Claims.First(c => c.Type == Claims.Sub).Value;
            var groups = await GetGroupsForAuthenticatedUser(subjectId);

            var permissions = await _permissionService.GetPermissionsForUser(
                Context.CurrentUser.Claims.First(c => c.Type == Claims.Sub).Value,
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

        private async Task<string[]> GetGroupsForAuthenticatedUser(string subjectId)
        {
            var userClaims = Context.CurrentUser?.Claims
                .Where(c => c.Type == "role" || c.Type == "groups")
                .Distinct(new ClaimComparer())
                .Select(c => c.Value.ToString());

            var groups = new List<string>();
            try
            {
                groups = (await _userService.GetGroupsForUser(subjectId)).ToList();
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