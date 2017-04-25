using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.API.Models;
using Microsoft.Extensions.Logging;
using Nancy;
using Nancy.ModelBinding;

namespace Fabric.Authorization.API.Modules
{
    public class UsersModule : NancyModule
    {
        public UsersModule() : base("/users")
        {
            Get("/{userId}/permissions", parameters =>
            {
                var userPermissionRequest = this.Bind<UserPermissionRequest>();
                return new UserPermissionsResponse
                {
                    RequestedGrain = userPermissionRequest.Grain,
                    RequestedResource = userPermissionRequest.Resource,
                    UserId = userPermissionRequest.UserId,
                    Permissions = new List<string>
                    {
                        "app/patientsafety.manageusers",
                        "app/patientsafety.createalerts"
                    }
                };
            });
        }
    }
}
