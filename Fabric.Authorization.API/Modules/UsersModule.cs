using Fabric.Authorization.API.Models;
using Fabric.Authorization.Domain;
using Fabric.Authorization.Domain.Exceptions;
using Nancy;
using Nancy.ModelBinding;

namespace Fabric.Authorization.API.Modules
{
    public class UsersModule : NancyModule
    {
        public UsersModule(IPermissionService permissionService) : base("/users")
        {
            Get("/{userId}/permissions", parameters =>
            {
                try
                {
                    var userPermissionRequest = this.Bind<UserPermissionRequest>();
                    var permissions = permissionService.GetPermissionsForUser(userPermissionRequest.UserId,
                        userPermissionRequest.Grain, userPermissionRequest.Resource);
                    return new UserPermissionsResponse
                    {
                        RequestedGrain = userPermissionRequest.Grain,
                        RequestedResource = userPermissionRequest.Resource,
                        UserId = userPermissionRequest.UserId,
                        Permissions = permissions
                    };
                }
                catch (UserNotFoundException)
                {
                    return HttpStatusCode.NotFound;
                }
            });
        }
    }
}
