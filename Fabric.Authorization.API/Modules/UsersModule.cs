using System;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Domain.Validators;
using IdentityModel;
using Nancy.ModelBinding;
using Serilog;

namespace Fabric.Authorization.API.Modules
{
    public class UsersModule : FabricModule<User>
    {
        private readonly GroupService _groupService;
        private readonly ClientService _clientService;

        public UsersModule(ClientService clientService, GroupService groupService, UserValidator validator, ILogger logger) : base("/user", logger, validator)
        {
            _groupService = groupService ?? throw new ArgumentNullException(nameof(groupService));
            _clientService = clientService ?? throw new ArgumentNullException(nameof(clientService));

            //Get all the permissions for a user
            Get("/permissions", async parameters => await this.GetUserPermissions());
        }

        private async Task<dynamic> GetUserPermissions()
        {
            var userPermissionRequest = this.Bind<UserInfoRequest>();
            await this.SetDefaultRequest(userPermissionRequest);

            CheckAccess(_clientService, userPermissionRequest.Grain, userPermissionRequest.SecurableItem, AuthorizationReadClaim);
            var groups = this.GetGroupsForAuthenticatedUser();
            var permissions = await _groupService.GetPermissionsForGroups(groups,
                userPermissionRequest.Grain, userPermissionRequest.SecurableItem);

            return new UserPermissionsApiModel
            {
                RequestedGrain = userPermissionRequest.Grain,
                RequestedSecurableItem = userPermissionRequest.SecurableItem,
                Permissions = permissions
            };
        }

        private string[] GetGroupsForAuthenticatedUser()
        {
            return Context.CurrentUser?.Claims.Where(c => c.Type == "role" || c.Type == "groups").Distinct(new ClaimComparer()).Select(c => c.Value.ToString()).ToArray();
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