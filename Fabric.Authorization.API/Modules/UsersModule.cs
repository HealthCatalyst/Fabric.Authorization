﻿using Fabric.Authorization.API.Models;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Services;
using Nancy;
using Nancy.ModelBinding;

namespace Fabric.Authorization.API.Modules
{
    public class UsersModule : NancyModule
    {
        public UsersModule(IGroupService groupService) : base("/user")
        {
            //Get all the permissions for a user
            Get("/permissions", parameters =>
            {
                try
                {
                    //TODO: validate that the client has access to the grain/securableItem they are requesting permissions for
                    var userPermissionRequest = this.Bind<UserInfoRequest>();
                    var groups = new[] { "HC PatientSafety Admin", "HC SourceMartDesigner Admin" }; //TODO: get this from the identity when we wire up that functionality
                    var permissions = groupService.GetPermissionsForGroups(groups,
                        userPermissionRequest.Grain, userPermissionRequest.SecurableItem);
                    return new UserPermissionsApiModel
                    {
                        RequestedGrain = userPermissionRequest.Grain,
                        RequestedSecurableItem = userPermissionRequest.SecurableItem,
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
