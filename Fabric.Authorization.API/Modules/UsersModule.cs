using System;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.Domain;
using Fabric.Authorization.Domain.Exceptions;
using Nancy;
using Nancy.ModelBinding;

namespace Fabric.Authorization.API.Modules
{
    public class UsersModule : NancyModule
    {
        public UsersModule(IUserService userService) : base("/users")
        {
            Get("/{userId}/permissions", parameters =>
            {
                try
                {
                    var userPermissionRequest = this.Bind<UserInfoRequest>();
                    var permissions = userService.GetPermissionsForUser(userPermissionRequest.UserId,
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

            Get("/{userId}/roles", parameters =>
            {
                try
                {
                    var userRoleRequest = this.Bind<UserInfoRequest>();
                    var roles = userService.GetRolesForUser(userRoleRequest.UserId, userRoleRequest.Grain,
                        userRoleRequest.Resource);
                    return new UserRoleResponse
                    {
                        RequestedGrain = userRoleRequest.Grain,
                        RequestedResource = userRoleRequest.Resource,
                        UserId = userRoleRequest.UserId,
                        Roles = roles
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
