using System;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.Domain.Services;
using Nancy.ModelBinding;
using System.Linq;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Validators;
using IdentityModel;
using Serilog;

namespace Fabric.Authorization.API.Modules
{
    public class UsersModule : FabricModule<User>
    {
        private readonly IGroupService _groupService;
        private readonly IClientService _clientService;
        public UsersModule(IClientService clientService, IGroupService groupService, ILogger logger, UserValidator validator) : base("/user", logger, validator)
        {
            _groupService = groupService ?? throw new ArgumentNullException(nameof(groupService));
            _clientService = clientService ?? throw new ArgumentNullException(nameof(clientService));

            //Get all the permissions for a user
            Get("/permissions", parameters => GetUserPermissions());
        }

        private dynamic GetUserPermissions()
        {
            var userPermissionRequest = this.Bind<UserInfoRequest>();
            SetDefaultRequest(userPermissionRequest);

            CheckAccess(_clientService, userPermissionRequest.Grain, userPermissionRequest.SecurableItem,
                AuthorizationReadClaim);
            var groups = GetGroupsForAuthenticatedUser();
            var permissions = _groupService.GetPermissionsForGroups(groups,
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

        private void SetDefaultRequest(UserInfoRequest request)
        {
            if (string.IsNullOrEmpty(request.Grain) && string.IsNullOrEmpty(request.SecurableItem))
            {
                var client = _clientService.GetClient(ClientId);
                request.Grain = Constants.TopLevelGrains.AppGrain;
                request.SecurableItem = client.TopLevelSecurableItem.Name;
            }
        }
    }
}
