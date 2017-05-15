using System;
using System.Collections.Generic;
using System.Linq;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Domain.Validators;
using Nancy;
using Nancy.ModelBinding;
using Serilog;

namespace Fabric.Authorization.API.Modules
{
    public class PermissionsModule : FabricModule<Permission>
    {
        private readonly IClientService _clientService;
        private readonly IPermissionService _permissionService;

        public PermissionsModule(IPermissionService permissionService,
            IClientService clientService,
            ILogger logger,
            PermissionValidator validator) : base("/Permissions", logger, validator)
        {
            //private members
            _clientService = clientService ?? throw new ArgumentNullException(nameof(clientService));
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));

            //routes and handlers
            Get("/{grain}/{securableItem}", parameters => GetPermissionsForSecurableItem(parameters));
            Get("/{grain}/{securableItem}/{permissionName}", parameters => GetPermissionByName(parameters));
            Get("/{permissionId}", parameters => GetPermissionById(parameters));
            Post("/", parameters => AddPermission());
            Delete("/{permissionId}", parameters => DeletePermission(parameters));
        }

        private dynamic GetPermissionsForSecurableItem(dynamic parameters)
        {
            CheckAccess(_clientService, parameters.grain, parameters.securableItem, AuthorizationReadClaim);
            IEnumerable<Permission> permissions =
                _permissionService.GetPermissions(parameters.grain, parameters.securableItem);
            return permissions.Select(p => p.ToPermissionApiModel());
        }

        private dynamic GetPermissionByName(dynamic parameters)
        {
            CheckAccess(_clientService, parameters.grain, parameters.securableItem, AuthorizationReadClaim);
            IEnumerable<Permission> permissions = _permissionService.GetPermissions(parameters.grain, parameters.securableItem, parameters.permissionName);
            return permissions.Select(p => p.ToPermissionApiModel());
        }

        private dynamic GetPermissionById(dynamic parameters)
        {
            try
            {
                if (!Guid.TryParse(parameters.permissionId, out Guid permissionId))
                {
                    return CreateFailureResponse("permissionId must be a guid.", HttpStatusCode.BadRequest);
                }

                Permission permission = _permissionService.GetPermission(permissionId);
                CheckAccess(_clientService, permission.Grain, permission.SecurableItem, AuthorizationReadClaim);
                return permission.ToPermissionApiModel();
            }
            catch (PermissionNotFoundException ex)
            {

                Logger.Error(ex, ex.Message, parameters.permissionId);
                return CreateFailureResponse($"The specified permission with id: {parameters.permissionId} was not found.", HttpStatusCode.NotFound);
            }
        }
                
        private dynamic AddPermission()
        {
            var permissionApiModel = this.Bind<PermissionApiModel>(binderIgnore => binderIgnore.Id,
                binderIgnore => binderIgnore.CreatedBy,
                binderIgnore => binderIgnore.CreatedDateTimeUtc,
                binderIgnore => binderIgnore.ModifiedDateTimeUtc,
                binderIgnore => binderIgnore.ModifiedBy);

            var incomingPermission = permissionApiModel.ToPermissionDomainModel();
            Validate(incomingPermission);
            CheckAccess(_clientService, permissionApiModel.Grain, permissionApiModel.SecurableItem, AuthorizationWriteClaim);
            Permission permission = _permissionService.AddPermission(incomingPermission);
            return CreateSuccessfulPostResponse(permission.ToPermissionApiModel());
        }

        private dynamic DeletePermission(dynamic parameters)
        {
            try
            {
                if (!Guid.TryParse(parameters.permissionId, out Guid permissionId))
                {
                    return CreateFailureResponse("permissionId must be a guid.", HttpStatusCode.BadRequest);
                }
                Permission permission = _permissionService.GetPermission(permissionId);
                CheckAccess(_clientService, permission.Grain, permission.SecurableItem, AuthorizationReadClaim);
                _permissionService.DeletePermission(permission);
                return HttpStatusCode.NoContent;
            }
            catch (PermissionNotFoundException ex)
            {
                Logger.Error(ex, ex.Message, parameters.permissionId);
                return CreateFailureResponse($"The specified permission with id: {parameters.permissionId} was not found.", HttpStatusCode.NotFound);
            }
        }
    }
}
