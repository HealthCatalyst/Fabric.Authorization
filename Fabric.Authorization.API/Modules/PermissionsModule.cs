using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.Domain;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Permissions;
using Nancy;
using Nancy.ModelBinding;

namespace Fabric.Authorization.API.Modules
{
    public class PermissionsModule : NancyModule
    {
        public PermissionsModule(IPermissionService permissionService) : base("/Permissions")
        {
            Get("/{grain}/{resource}", parameters =>
            {
                IEnumerable<Permission> permissions = permissionService.GetPermissions(parameters.grain, parameters.resource);
                return permissions.Select(p => p.ToPermissionApiModel());
            });

            Get("/{grain}/{resource}/{permissionName}", parameters =>
            {
                IEnumerable<Permission> permissions = permissionService.GetPermissions(parameters.grain, parameters.resource, parameters.permissionName);
                return permissions.Select(p => p.ToPermissionApiModel());
            });

            Get("/{permissionId}", parameters =>
            {
                var permission = permissionService.GetPermission(parameters.permissionId);
                return permission;
            });

            Post("/", parameters =>
            {
                try
                {
                    var permissionApiModel = this.Bind<PermissionApiModel>();
                     var permission = permissionService.AddPermission(permissionApiModel.Grain, permissionApiModel.Resource,
                        permissionApiModel.Name);
                    return Negotiate.WithModel(permission).WithStatusCode(HttpStatusCode.Created);
                }
                catch (PermissionAlreadyExistsException)
                {
                    return HttpStatusCode.BadRequest;
                }
            });

            Delete("/{permissionId}", parameters =>
            {
                try
                {
                    permissionService.DeletePermission(parameters.permissionId);
                    return HttpStatusCode.NoContent;
                }
                catch (PermissionNotFoundException)
                {
                    return HttpStatusCode.NotFound;
                }
            });
        }
    }
}
