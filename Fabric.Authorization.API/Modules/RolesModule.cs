using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.API.Services;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Domain.Validators;
using Nancy;
using Nancy.ModelBinding;
using Serilog;

namespace Fabric.Authorization.API.Modules
{
    public class RolesModule : FabricModule<Role>
    {
        private readonly ClientService _clientService;
        private readonly RoleService _roleService;
        private readonly GrainService _grainService;

        public RolesModule(
            RoleService roleService,
            ClientService clientService,
            GrainService grainService,
            RoleValidator validator,
            AccessService accessService,
            ILogger logger) : base("/v1/roles", logger, validator, accessService)
        {
            _roleService = roleService ?? throw new ArgumentNullException(nameof(roleService));
            _clientService = clientService ?? throw new ArgumentNullException(nameof(clientService));
            _grainService = grainService ?? throw new ArgumentNullException(nameof(grainService));

            Post("/",
                async parameters => await AddRole().ConfigureAwait(false),
                null,
                "AddRole");

            Delete("/{roleId}",
                async parameters => await this.DeleteRole(parameters).ConfigureAwait(false),
                null,
                "DeleteRole");

            Get("/{grain}/{securableItem}",
                async parameters => await this.GetRolesForSecurableItem(parameters).ConfigureAwait(false),
                null,
                "GetRolesBySecurableItem");

            Get("/{grain}/{securableItem}/{roleName}",
                async parameters => await this.GetRoleByName(parameters).ConfigureAwait(false),
                null,
                "GetRoleByName");

            Post("/{roleId}/permissions",
                async parameters => await this.AddPermissionsToRole(parameters).ConfigureAwait(false),
                null,
                "AddPermissionsToRole");

            Delete("/{roleId}/permissions",
                async parameters => await this.DeletePermissionsFromRole(parameters).ConfigureAwait(false),
                null,
                "DeletePermissionsFromRole");
        }

        private async Task<dynamic> GetRolesForSecurableItem(dynamic parameters)
        {
            CheckReadAccess();
            IEnumerable<Role> roles = await _roleService.GetRoles(parameters.grain, parameters.securableItem);
            return roles.Select(r => r.ToRoleApiModel());
        }

        private async Task<dynamic> GetRoleByName(dynamic parameters)
        {
            CheckReadAccess();
            IEnumerable<Role> roles =
                await _roleService.GetRoles(parameters.grain, parameters.securableItem, parameters.roleName);
            return roles.Select(r => r.ToRoleApiModel());
        }

        private async Task<dynamic> AddRole()
        {
            var roleApiModel = this.Bind<RoleApiModel>(binderIgnore => binderIgnore.Id,
                binderIgnore => binderIgnore.CreatedBy,
                binderIgnore => binderIgnore.CreatedDateTimeUtc,
                binderIgnore => binderIgnore.ModifiedDateTimeUtc,
                binderIgnore => binderIgnore.ModifiedBy);

            var incomingRole = roleApiModel.ToRoleDomainModel();
            Validate(incomingRole);
            await CheckWriteAccess(_clientService, _grainService, roleApiModel.Grain, roleApiModel.SecurableItem);
            var role = await _roleService.AddRole(incomingRole);
            return CreateSuccessfulPostResponse(role.ToRoleApiModel());
        }

        private async Task<dynamic> DeleteRole(dynamic parameters)
        {
            try
            {
                if (!Guid.TryParse(parameters.roleId, out Guid roleId))
                {
                    return CreateFailureResponse("roleId must be a guid.", HttpStatusCode.BadRequest);
                }

                var roleToDelete = await _roleService.GetRole(roleId);
                await CheckWriteAccess(_clientService, _grainService, roleToDelete.Grain, roleToDelete.SecurableItem);
                await _roleService.DeleteRole(roleToDelete);
                return HttpStatusCode.NoContent;
            }
            catch (NotFoundException<Role> ex)
            {
                Logger.Error(ex, ex.Message, parameters.roleId);
                return CreateFailureResponse(ex.Message, HttpStatusCode.NotFound);
            }
        }

        private async Task<dynamic> AddPermissionsToRole(dynamic parameters)
        {
            try
            {
                var permissionApiModels = this.Bind<List<PermissionApiModel>>(new BindingConfig { BodyOnly = true });

                if (!Guid.TryParse(parameters.roleId, out Guid roleId))
                {
                    return CreateFailureResponse("roleId must be a guid.", HttpStatusCode.BadRequest);
                }

                if (permissionApiModels.Count == 0)
                {
                    return CreateFailureResponse(
                        "No permissions specified to add, ensure an array of permissions is included in the request.",
                        HttpStatusCode.BadRequest);
                }

                if (permissionApiModels.Any(p => p.Id == null))
                {
                    return CreateFailureResponse(
                        "Permission id is required but missing in the request, ensure each permission has an id.",
                        HttpStatusCode.BadRequest);
                }

                var roleToUpdate = await _roleService.GetRole(roleId);
                await CheckWriteAccess(_clientService, _grainService, roleToUpdate.Grain, roleToUpdate.SecurableItem);
                var updatedRole = await _roleService.AddPermissionsToRole(
                                      roleToUpdate,
                                      permissionApiModels.Where(p => p.Id.HasValue).Select(p => p.Id.Value).ToArray(), new Guid[]{});
                return CreateSuccessfulPostResponse(updatedRole.ToRoleApiModel());
            }
            catch (NotFoundException<Role> ex)
            {
                return CreateFailureResponse(ex.Message, HttpStatusCode.NotFound);
            }
            catch (NotFoundException<Permission> ex)
            {
                return CreateFailureResponse(ex.Message, HttpStatusCode.NotFound);
            }
            catch (IncompatiblePermissionException ex)
            {
                return CreateFailureResponse(ex.Message, HttpStatusCode.BadRequest);
            }
            catch (AlreadyExistsException<Permission> ex)
            {
                return CreateFailureResponse(ex.Message, HttpStatusCode.Conflict);
            }
        }

        private async Task<dynamic> DeletePermissionsFromRole(dynamic parameters)
        {
            try
            {
                var permissionApiModels = this.Bind<List<PermissionApiModel>>(new BindingConfig {BodyOnly = true});

                if (!Guid.TryParse(parameters.roleId, out Guid roleId))
                {
                    return CreateFailureResponse("roleId must be a guid.", HttpStatusCode.BadRequest);
                }

                if (permissionApiModels.Count == 0)
                {
                    return CreateFailureResponse(
                        "No permissions specified to add, ensure an array of permissions is included in the request.",
                        HttpStatusCode.BadRequest);
                }

                var roleToUpdate = await _roleService.GetRole(roleId);
                await CheckWriteAccess(_clientService, _grainService, roleToUpdate.Grain, roleToUpdate.SecurableItem);
                var updatedRole = await _roleService.RemovePermissionsFromRole(roleToUpdate,
                    permissionApiModels.Where(p => p.Id.HasValue).Select(p => p.Id.Value).ToArray());
                return CreateSuccessfulPostResponse(updatedRole.ToRoleApiModel());
            }
            catch (NotFoundException<Role> ex)
            {
                return CreateFailureResponse(ex.Message, HttpStatusCode.NotFound);
            }
            catch (NotFoundException<Permission> ex)
            {
                return CreateFailureResponse(ex.Message, HttpStatusCode.NotFound);
            }
        }
    }
}