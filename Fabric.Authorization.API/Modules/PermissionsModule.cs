using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly ClientService _clientService;
        private readonly PermissionService _permissionService;

        public PermissionsModule(PermissionService permissionService,
            ClientService clientService,
            PermissionValidator validator,
            ILogger logger) : base("/Permissions", logger, validator)
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
                _permissionService.GetPermissions(parameters.grain, parameters.securableItem).Result;
            return permissions.Select(p => p.ToPermissionApiModel());
        }

        private dynamic GetPermissionByName(dynamic parameters)
        {
            CheckAccess(_clientService, parameters.grain, parameters.securableItem, AuthorizationReadClaim);
            IEnumerable<Permission> permissions = _permissionService.GetPermissions(parameters.grain, parameters.securableItem, parameters.permissionName).Result;
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

                Permission permission = _permissionService.GetPermission(permissionId).Result;
                CheckAccess(_clientService, permission.Grain, permission.SecurableItem, AuthorizationReadClaim);
                return permission.ToPermissionApiModel();
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException is NotFoundException<Permission>)
                {
                    Logger.Error(ex, ex.Message, parameters.permissionId);
                    return CreateFailureResponse($"The specified permission with id: {parameters.permissionId} was not found",
                        HttpStatusCode.NotFound);
                }
                else
                {
                    throw;
                }
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

            Permission permission = _permissionService.AddPermission(incomingPermission).Result;
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
                Permission permission = _permissionService.GetPermission(permissionId).Result;
                CheckAccess(_clientService, permission.Grain, permission.SecurableItem, AuthorizationWriteClaim);
                _permissionService.DeletePermission(permission).Wait();
                return HttpStatusCode.NoContent;
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException is NotFoundException<Permission>)
                {
                    Logger.Error(ex, ex.Message, parameters.permissionId);
                    return CreateFailureResponse($"The specified permission with id: {parameters.permissionId} was not found",
                        HttpStatusCode.NotFound);
                }
                else
                {
                    throw;
                }
            }
        }
    }
}