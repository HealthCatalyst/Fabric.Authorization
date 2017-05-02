using System;
using System.Collections.Generic;
using System.Linq;
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
                if (!Guid.TryParse(parameters.permissionId, out Guid permissionId))
                {
                    return HttpStatusCode.BadRequest;
                }

                Permission permission = permissionService.GetPermission(permissionId);
                return permission.ToPermissionApiModel();
            });

            Post("/", parameters =>
            {
                try
                {
                    var permissionApiModel = this.Bind<PermissionApiModel>();
                    Result<Permission> result = permissionService.AddPermission<Permission>(permissionApiModel.Grain, permissionApiModel.Resource,
                       permissionApiModel.Name);
                    if (result.ValidationResult.IsValid)
                    {
                        return Negotiate.WithModel(result.Model.ToPermissionApiModel()).WithStatusCode(HttpStatusCode.Created);
                    }
                    return Negotiate.WithModel(result.ValidationResult).WithStatusCode(HttpStatusCode.BadRequest);
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
                    if (!Guid.TryParse(parameters.permissionId, out Guid permissionId))
                    {
                        return HttpStatusCode.BadRequest;
                    }

                    permissionService.DeletePermission(permissionId);
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
