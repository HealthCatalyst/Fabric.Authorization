using System;
using System.Collections.Generic;
using System.Linq;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.Domain;
using Fabric.Authorization.Domain.Clients;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Permissions;
using Nancy;
using Nancy.ModelBinding;

namespace Fabric.Authorization.API.Modules
{
    public class PermissionsModule : FabricModule
    {
        public PermissionsModule(IPermissionService permissionService, IClientService clientService) : base("/Permissions")
        {
            Get("/{grain}/{resource}", parameters =>
            {
                CheckAccess<Permission>(clientService, parameters.grain, parameters.resource, AuthorizationReadClaim);
                IEnumerable<Permission> permissions =
                    permissionService.GetPermissions(parameters.grain, parameters.resource);
                return permissions.Select(p => p.ToPermissionApiModel());
            });

            Get("/{grain}/{resource}/{permissionName}", parameters =>
            {
                CheckAccess<Permission>(clientService, parameters.grain, parameters.resource, AuthorizationReadClaim);
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
                    CheckAccess<Permission>(clientService, permission.Grain, permission.Resource, AuthorizationReadClaim);
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

                var validationResult = permissionService.ValidatePermission(permissionApiModel.Grain,
                    permissionApiModel.Resource, permissionApiModel.Name);

                if (!validationResult.ValidationResult.IsValid)
                {
                    return CreateFailureResponse<Permission>(validationResult.ValidationResult, HttpStatusCode.BadRequest);
                }
                CheckAccess<Permission>(clientService, permissionApiModel.Grain, permissionApiModel.Resource, AuthorizationWriteClaim);
                Permission permission = permissionService.AddPermission(permissionApiModel.Grain, permissionApiModel.Resource,
                   permissionApiModel.Name);
                return CreateSuccessfulPostResponse(permission.ToPermissionApiModel());
            });

            Delete("/{permissionId}", parameters =>
            {
                try
                {
                    if (!Guid.TryParse(parameters.permissionId, out Guid permissionId))
                    {
                        return CreateFailureResponse<Permission>("permissionId must be a guid.", HttpStatusCode.BadRequest);
                    }
                    Permission permission = permissionService.GetPermission(permissionId);
                    CheckAccess<Permission>(clientService, permission.Grain, permission.Resource, AuthorizationReadClaim);
                    permissionService.DeletePermission(permission);
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
