using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Domain.Stores.Services;
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
            ILogger logger) : base("/v1/Permissions", logger, validator)
        {
            //private members
            _clientService = clientService ?? throw new ArgumentNullException(nameof(clientService));
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));

            //routes and handlers
            Get("/{grain}/{securableItem}", async parameters => await this.GetPermissionsForSecurableItem(parameters).ConfigureAwait(false));
            Get("/{grain}/{securableItem}/{permissionName}", async parameters => await this.GetPermissionByName(parameters).ConfigureAwait(false));
            Get("/{permissionId}", async parameters => await this.GetPermissionById(parameters).ConfigureAwait(false));
            Post("/", async parameters => await this.AddPermission().ConfigureAwait(false));
            Delete("/{permissionId}", async parameters => await this.DeletePermission(parameters).ConfigureAwait(false));
        }

        private async Task<dynamic> GetPermissionsForSecurableItem(dynamic parameters)
        {
            await CheckAccess(_clientService, parameters.grain, parameters.securableItem, AuthorizationReadClaim);
            IEnumerable<Permission> permissions = await _permissionService.GetPermissions(parameters.grain, parameters.securableItem);
            return permissions.Select(p => p.ToPermissionApiModel());
        }

        private async Task<dynamic> GetPermissionByName(dynamic parameters)
        {
            await CheckAccess(_clientService, parameters.grain, parameters.securableItem, AuthorizationReadClaim);
            IEnumerable<Permission> permissions = await _permissionService.GetPermissions(parameters.grain, parameters.securableItem, parameters.permissionName);
            return permissions.Select(p => p.ToPermissionApiModel());
        }

        private async Task<dynamic> GetPermissionById(dynamic parameters)
        {
            try
            {
                if (!Guid.TryParse(parameters.permissionId, out Guid permissionId))
                {
                    return CreateFailureResponse("permissionId must be a guid.", HttpStatusCode.BadRequest);
                }

                Permission permission = await _permissionService.GetPermission(permissionId);
                await CheckAccess(_clientService, permission.Grain, permission.SecurableItem, AuthorizationReadClaim);
                return permission.ToPermissionApiModel();
            }
            catch (NotFoundException<Permission> ex)
            {
                Logger.Error(ex, ex.Message, parameters.permissionId);
                return CreateFailureResponse($"The specified permission with id: {parameters.permissionId} was not found",
                    HttpStatusCode.NotFound);
            }
        }

        private async Task<dynamic> AddPermission()
        {
            var permissionApiModel = this.Bind<PermissionApiModel>(binderIgnore => binderIgnore.Id,
                binderIgnore => binderIgnore.CreatedBy,
                binderIgnore => binderIgnore.CreatedDateTimeUtc,
                binderIgnore => binderIgnore.ModifiedDateTimeUtc,
                binderIgnore => binderIgnore.ModifiedBy);

            var incomingPermission = permissionApiModel.ToPermissionDomainModel();

            Validate(incomingPermission);
            await CheckAccess(_clientService, permissionApiModel.Grain, permissionApiModel.SecurableItem, AuthorizationWriteClaim);

            Permission permission = await _permissionService.AddPermission(incomingPermission);
            return CreateSuccessfulPostResponse(permission.ToPermissionApiModel());
        }

        private async Task<dynamic> DeletePermission(dynamic parameters)
        {
            try
            {
                if (!Guid.TryParse(parameters.permissionId, out Guid permissionId))
                {
                    return CreateFailureResponse("permissionId must be a guid.", HttpStatusCode.BadRequest);
                }
                Permission permission = await _permissionService.GetPermission(permissionId);
                await CheckAccess(_clientService, permission.Grain, permission.SecurableItem, AuthorizationWriteClaim);
                await _permissionService.DeletePermission(permission);
                return HttpStatusCode.NoContent;
            }
            catch (NotFoundException<Permission> ex)
            {
                Logger.Error(ex, ex.Message, parameters.permissionId);
                return CreateFailureResponse($"The specified permission with id: {parameters.permissionId} was not found",
                    HttpStatusCode.NotFound);
            }

        }
    }
}