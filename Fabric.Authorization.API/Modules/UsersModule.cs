using System;
using System.Linq;
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
            //Get all the permissions for a user
            Get("/{userId}/permissions", parameters =>
            {
                try
                {
                    var userPermissionRequest = this.Bind<UserInfoRequest>();
                    var permissions = userService.GetPermissionsForUser(userPermissionRequest.UserId,
                        userPermissionRequest.Grain, userPermissionRequest.Resource);
                    return new UserPermissionsApiModel
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

            //Get all the roles for a user
            Get("/{userId}/roles", parameters =>
            {
                try
                {
                    var userRoleRequest = this.Bind<UserInfoRequest>();
                    var roles = userService.GetRolesForUser(userRoleRequest.UserId, userRoleRequest.Grain,
                        userRoleRequest.Resource);
                    return new UserRoleApiModel
                    {
                        RequestedGrain = userRoleRequest.Grain,
                        RequestedResource = userRoleRequest.Resource,
                        UserId = userRoleRequest.UserId,
                        Roles = roles.Select(r => r.ToRoleApiModel())
                    };
                }
                catch (UserNotFoundException)
                {
                    return HttpStatusCode.NotFound;
                }
            });

            //Add a role to a user
            Post("/{userId}/roles", parameters =>
            {
                try
                {
                    var roleApiModel = this.Bind<RoleApiModel>();
                    userService.AddRoleToUser(parameters.userId, roleApiModel.Id, roleApiModel.Grain,
                        roleApiModel.Resource,
                        roleApiModel.Name);
                    return HttpStatusCode.NoContent;
                }
                catch (UserNotFoundException)
                {
                    return HttpStatusCode.NotFound;
                }
                catch (RoleNotFoundException)
                {
                    return HttpStatusCode.BadRequest;
                }
            });

            Delete("/{userId}/roles", parameters =>
            {
                try
                {
                    var roleApiModel = this.Bind<RoleApiModel>();
                    userService.DeleteRoleFromUser(parameters.userId, roleApiModel.Id, roleApiModel.Grain,
                        roleApiModel.Resource,
                        roleApiModel.Name);
                    return HttpStatusCode.NoContent;
                }
                catch (UserNotFoundException)
                {
                    return HttpStatusCode.NotFound;
                }
                catch (RoleNotFoundException)
                {
                    return HttpStatusCode.BadRequest;
                }
            });
        }
    }
}
