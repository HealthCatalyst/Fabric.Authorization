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
    public class PermissionsModule : FabricModule
    {
        public PermissionsModule(IPermissionService permissionService) : base("/Permissions")
        {
            Get("/{grain}/{resource}", parameters =>
            {
                IEnumerable<Permission> permissions =
                    permissionService.GetPermissions(parameters.grain, parameters.resource);
                return permissions.Select(p => p.ToPermissionApiModel());
            });

            Get("/{grain}/{resource}/{permissionName}", parameters =>
            {
                IEnumerable<Permission> permissions = permissionService.GetPermissions(parameters.grain, parameters.resource, parameters.permissionName);
                return permissions.Select(p => p.ToPermissionApiModel());
            });

            Get("/{permissionId}", parameters =>
            {
                try
                {
                    if (!Guid.TryParse(parameters.permissionId, out Guid permissionId))
                    {
                        return CreateFailureResponse<Permission>("permissionId must be a guid.", HttpStatusCode.BadRequest);
                    }

                    Permission permission = permissionService.GetPermission(permissionId);
                    return permission.ToPermissionApiModel();
                }
                catch (PermissionNotFoundException)
                {
                    return CreateFailureResponse<Permission>("The specified permission was not found.", HttpStatusCode.NotFound);
                }
            });

            Post("/", parameters =>
            {
                var permissionApiModel = this.Bind<PermissionApiModel>();
                Result<Permission> result = permissionService.AddPermission(permissionApiModel.Grain, permissionApiModel.Resource,
                   permissionApiModel.Name);
                return result.ValidationResult.IsValid
                    ? CreateSuccessfulPostResponse(result.Model.ToPermissionApiModel())
                    : CreateFailureResponse<Permission>(result.ValidationResult, HttpStatusCode.BadRequest);
            });

            Delete("/{permissionId}", parameters =>
            {
                try
                {
                    if (!Guid.TryParse(parameters.permissionId, out Guid permissionId))
                    {
                        return CreateFailureResponse<Permission>("permissionId must be a guid.", HttpStatusCode.BadRequest);
                    }

                    permissionService.DeletePermission(permissionId);
                    return HttpStatusCode.NoContent;
                }
                catch (PermissionNotFoundException)
                {
                    return CreateFailureResponse<Permission>("The specified permission was not found.", HttpStatusCode.NotFound);
                }
            });
        }
    }
}
