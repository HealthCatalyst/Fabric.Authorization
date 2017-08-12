using System;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores.Services;
using Fabric.Authorization.Domain.Validators;
using IdentityModel;
using Nancy;
using Nancy.ModelBinding;
using Serilog;


namespace Fabric.Authorization.API.Modules
{
    public class UsersModule : FabricModule<User>
    {
        private readonly PermissionService _permissionService;
        private readonly ClientService _clientService;

        public UsersModule(ClientService clientService, PermissionService permissionService, UserValidator validator, ILogger logger) : base("/v1/user", logger, validator)
        {
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
            _clientService = clientService ?? throw new ArgumentNullException(nameof(clientService));

            // Get all the permissions for a user
            Get("/permissions", async _ => await this.GetUserPermissions().ConfigureAwait(false));
            Post("/{userId}/AdditionalPermissions", async param => await this.AddGranularPermissions(param, denied: false).ConfigureAwait(false));
            Post("/{userId}/DeniedPermissions", async param => await this.AddGranularPermissions(param, denied: true).ConfigureAwait(false));
        }

        private async Task<dynamic> AddGranularPermissions(dynamic param, bool denied)
        {
            var apiModel = this.Bind<GranularPermissionApiModel>();

            if (apiModel.Target != param["userId"])
            {
                return CreateFailureResponse("Target must be the user id.", HttpStatusCode.BadRequest);
            }

            foreach (var perm in apiModel.Permissions)
            {
                await CheckAccess(_clientService, perm.Grain, perm.SecurableItem, AuthorizationManageClientsClaim);
            }

            var granularPermissions = apiModel.ToGranularPermissionDomainModel();

            if (denied)
            {
                granularPermissions.DeniedPermissions = apiModel.Permissions.Select(p => p.ToPermissionDomainModel());
            }
            else
            {
                granularPermissions.AdditionalPermissions = apiModel.Permissions.Select(p => p.ToPermissionDomainModel());
            }

            await _permissionService.AddUserGranularPermissions(granularPermissions);
            return HttpStatusCode.NoContent;
        }
       
        private async Task<dynamic> GetUserPermissions()
        {
            var userPermissionRequest = this.Bind<UserInfoRequest>();
            await this.SetDefaultRequest(userPermissionRequest);
            await CheckAccess(_clientService, userPermissionRequest.Grain, userPermissionRequest.SecurableItem, AuthorizationReadClaim);
            var groups = this.GetGroupsForAuthenticatedUser();

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

        private string[] GetGroupsForAuthenticatedUser()
        {
            var userClaims = Context.CurrentUser?.Claims.Where(c => c.Type == "role" || c.Type == "groups").Distinct(new ClaimComparer()).Select(c => c.Value.ToString()).ToArray();

            Logger.Information($"found claims for user: {string.Join(",", userClaims)}");

            return userClaims;
        }

        private async Task SetDefaultRequest(UserInfoRequest request)
        {
            if (string.IsNullOrEmpty(request.Grain) && string.IsNullOrEmpty(request.SecurableItem))
            {
                var client = await _clientService.GetClient(ClientId);
                request.Grain = Constants.TopLevelGrains.AppGrain;
                request.SecurableItem = client.TopLevelSecurableItem.Name;
            }
        }
    }
}