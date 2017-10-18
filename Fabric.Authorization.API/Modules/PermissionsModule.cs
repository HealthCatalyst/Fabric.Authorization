using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
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

        public PermissionsModule(
            PermissionService permissionService,
            ClientService clientService,
            PermissionValidator validator,
            ILogger logger) : base("/v1/permissions", logger, validator)
        {
            _clientService = clientService ?? throw new ArgumentNullException(nameof(clientService));
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));

            Get("/{permissionId}",
                async parameters => await this.GetPermissionById(parameters).ConfigureAwait(false),
                null,
                "GetPermissionById");

            Post("/",
                async parameters => await AddPermission().ConfigureAwait(false),
                null,
                "AddPermission");

            Delete("/{permissionId}",
                async parameters => await this.DeletePermission(parameters).ConfigureAwait(false),
                null,
                "DeletePermission");

            Get("/{grain}/{securableItem}",
                async parameters => await this.GetPermissionsForSecurableItem(parameters).ConfigureAwait(false),
                null,
                "GetPermissionsForSecurableItem");

            Get("/{grain}/{securableItem}/{permissionName}",
                async parameters => await this.GetPermissionByName(parameters).ConfigureAwait(false),
                null,
                "GetPermissionByName");
        }

        private async Task<dynamic> GetPermissionById(dynamic parameters)
        {
            try
            {
                if (!Guid.TryParse(parameters.permissionId, out Guid permissionId))
                {
                    return CreateFailureResponse("permissionId must be a guid.", HttpStatusCode.BadRequest);
                }

                var permission = await _permissionService.GetPermission(permissionId);
                await CheckAccess(_clientService, permission.Grain, permission.SecurableItem, AuthorizationReadClaim);
                return permission.ToPermissionApiModel();
            }
            catch (NotFoundException<Permission> ex)
            {
                Logger.Error(ex, ex.Message, parameters.permissionId);
                return CreateFailureResponse(
                    $"The specified permission with id: {parameters.permissionId} was not found",
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
            await CheckAccess(_clientService, permissionApiModel.Grain, permissionApiModel.SecurableItem,
                AuthorizationWriteClaim);

            var permission = await _permissionService.AddPermission(incomingPermission);
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
                var permission = await _permissionService.GetPermission(permissionId);
                await CheckAccess(_clientService, permission.Grain, permission.SecurableItem, AuthorizationWriteClaim);
                await _permissionService.DeletePermission(permission);
                return HttpStatusCode.NoContent;
            }
            catch (NotFoundException<Permission> ex)
            {
                Logger.Error(ex, ex.Message, parameters.permissionId);
                return CreateFailureResponse(
                    $"The specified permission with id: {parameters.permissionId} was not found",
                    HttpStatusCode.NotFound);
            }
        }

        private async Task<dynamic> GetPermissionsForSecurableItem(dynamic parameters)
        {
            await CheckAccess(_clientService, parameters.grain, parameters.securableItem, AuthorizationReadClaim);
            IEnumerable<Permission> permissions =
                await _permissionService.GetPermissions(parameters.grain, parameters.securableItem);
            return permissions.Select(p => p.ToPermissionApiModel());
        }

        private async Task<dynamic> GetPermissionByName(dynamic parameters)
        {
            await CheckAccess(_clientService, parameters.grain, parameters.securableItem, AuthorizationReadClaim);
            IEnumerable<Permission> permissions =
                await _permissionService.GetPermissions(parameters.grain, parameters.securableItem,
                    parameters.permissionName);
            return permissions.Select(p => p.ToPermissionApiModel());
        }
    }
}