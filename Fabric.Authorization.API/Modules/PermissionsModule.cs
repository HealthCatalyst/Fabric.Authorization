using System;
using System.Collections.Generic;
using System.Linq;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.Domain;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Domain.Validators;
using Nancy;
using Nancy.ModelBinding;

namespace Fabric.Authorization.API.Modules
{
    public class PermissionsModule : FabricModule<Permission>
    {
        public PermissionsModule(IPermissionService permissionService, IClientService clientService, PermissionValidator validator) : base("/Permissions", validator)
        {
            Get("/{grain}/{resource}", parameters =>
            {
                CheckAccess(clientService, parameters.grain, parameters.resource, AuthorizationReadClaim);
                IEnumerable<Permission> permissions =
                    permissionService.GetPermissions(parameters.grain, parameters.resource);
                return permissions.Select(p => p.ToPermissionApiModel());
            });

            Get("/{grain}/{resource}/{permissionName}", parameters =>
            {
                CheckAccess(clientService, parameters.grain, parameters.resource, AuthorizationReadClaim);
                IEnumerable<Permission> permissions = permissionService.GetPermissions(parameters.grain, parameters.resource, parameters.permissionName);
                return permissions.Select(p => p.ToPermissionApiModel());
            });

            Get("/{permissionId}", parameters =>
            {
                try
                {
                    if (!Guid.TryParse(parameters.permissionId, out Guid permissionId))
                    {
                        return CreateFailureResponse("permissionId must be a guid.", HttpStatusCode.BadRequest);
                    }

                    Permission permission = permissionService.GetPermission(permissionId);
                    CheckAccess(clientService, permission.Grain, permission.Resource, AuthorizationReadClaim);
                    return permission.ToPermissionApiModel();
                }
                catch (PermissionNotFoundException)
                {
                    return CreateFailureResponse("The specified permission was not found.", HttpStatusCode.NotFound);
                }
            });

            Post("/", parameters =>
            {
                var permissionApiModel = this.Bind<PermissionApiModel>();

                Validate(permissionApiModel.ToPermissionDomainModel());
                CheckAccess(clientService, permissionApiModel.Grain, permissionApiModel.Resource, AuthorizationWriteClaim);
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
                        return CreateFailureResponse("permissionId must be a guid.", HttpStatusCode.BadRequest);
                    }
                    Permission permission = permissionService.GetPermission(permissionId);
                    CheckAccess(clientService, permission.Grain, permission.Resource, AuthorizationReadClaim);
                    permissionService.DeletePermission(permission);
                    return HttpStatusCode.NoContent;
                }
                catch (PermissionNotFoundException)
                {
                    return CreateFailureResponse("The specified permission was not found.", HttpStatusCode.NotFound);
                }
            });
        }
    }
}
