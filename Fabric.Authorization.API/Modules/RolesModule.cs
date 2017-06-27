using System;
using System.Collections.Generic;
using System.Linq;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Services;
using Nancy;
using Nancy.ModelBinding;
using Serilog;
using Fabric.Authorization.Domain.Validators;

namespace Fabric.Authorization.API.Modules
{
    public class RolesModule : FabricModule<Role>
    {

        private readonly IRoleService _roleService;
        private readonly IClientService _clientService;

        public RolesModule(IRoleService roleService, IClientService clientService, ILogger logger, RoleValidator validator) : base("/roles", logger, validator)
        {
            //private members
            _roleService = roleService ?? throw new ArgumentNullException(nameof(roleService));
            _clientService = clientService ?? throw new ArgumentNullException(nameof(clientService));

            //routes and handlers
            Get("/{grain}/{securableItem}", parameters => GetRolesForSecurableItem(parameters));
            Get("/{grain}/{securableItem}/{roleName}", parameters => GetRoleByName(parameters));
            Post("/", parameters => AddRole());
            Delete("/{roleId}", parameters => DeleteRole(parameters));
            Post("/{roleId}/permissions", parameters => AddPermissionsToRole(parameters));
            Delete("/{roleId}/permissions", parameters => DeletePermissionsFromRole(parameters));
        }

        private dynamic GetRolesForSecurableItem(dynamic parameters)
        {
            CheckAccess(_clientService, parameters.grain, parameters.securableItem, AuthorizationReadClaim);
            IEnumerable<Role> roles = _roleService.GetRoles(parameters.grain, parameters.securableItem);
            return roles.Select(r => r.ToRoleApiModel());
        }

        private dynamic GetRoleByName(dynamic parameters)
        {
            CheckAccess(_clientService, parameters.grain, parameters.securableItem, AuthorizationReadClaim);
            IEnumerable<Role> roles = _roleService.GetRoles(parameters.grain, parameters.securableItem, parameters.roleName);
            return roles.Select(r => r.ToRoleApiModel());
        }

        private dynamic AddRole()
        {
            var roleApiModel = this.Bind<RoleApiModel>(binderIgnore => binderIgnore.Id,
                binderIgnore => binderIgnore.CreatedBy,
                binderIgnore => binderIgnore.CreatedDateTimeUtc,
                binderIgnore => binderIgnore.ModifiedDateTimeUtc,
                binderIgnore => binderIgnore.ModifiedBy);

            var incomingRole = roleApiModel.ToRoleDomainModel();
            Validate(incomingRole);
            CheckAccess(_clientService, roleApiModel.Grain, roleApiModel.SecurableItem, AuthorizationWriteClaim);
            Role role = _roleService.AddRole(incomingRole);
            return CreateSuccessfulPostResponse(role.ToRoleApiModel());
        }

        private dynamic DeleteRole(dynamic parameters)
        {
            try
            {
                if (!Guid.TryParse(parameters.roleId, out Guid roleId))
                {
                    return CreateFailureResponse("roleId must be a guid.", HttpStatusCode.BadRequest);
                }

                Role roleToDelete = _roleService.GetRole(roleId);
                CheckAccess(_clientService, roleToDelete.Grain, roleToDelete.SecurableItem, AuthorizationWriteClaim);
                _roleService.DeleteRole(roleToDelete);
                return HttpStatusCode.NoContent;
            }
            catch (RoleNotFoundException ex)
            {
                Logger.Error(ex, ex.Message, parameters.roleId);
                return CreateFailureResponse(ex.Message, HttpStatusCode.NotFound);
            }
        }

        private dynamic AddPermissionsToRole(dynamic parameters)
        {
            try
            {
                var roleApiModels = this.Bind<List<PermissionApiModel>>(new BindingConfig { BodyOnly = true });

                if (!Guid.TryParse(parameters.roleId, out Guid roleId))
                {
                    return CreateFailureResponse("roleId must be a guid.", HttpStatusCode.BadRequest);
                }

                if (roleApiModels.Count == 0)
                {
                    return CreateFailureResponse(
                        "No permissions specified to add, ensure an array of permissions is included in the request.",
                        HttpStatusCode.BadRequest);
                }

                Role roleToUpdate = _roleService.GetRole(roleId);
                CheckAccess(_clientService, roleToUpdate.Grain, roleToUpdate.SecurableItem, AuthorizationWriteClaim);
                Role updatedRole = _roleService.AddPermissionsToRole(roleToUpdate,
                    roleApiModels.Where(p => p.Id.HasValue).Select(p => p.Id.Value).ToArray());
                return CreateSuccessfulPostResponse(updatedRole.ToRoleApiModel(), HttpStatusCode.OK);
            }
            catch (RoleNotFoundException ex)
            {
                Logger.Error(ex, ex.Message, parameters.roleId);
                return CreateFailureResponse(ex.Message, HttpStatusCode.BadRequest);
            }
            catch (PermissionNotFoundException ex)
            {
                Logger.Error(ex, ex.Message);
                return CreateFailureResponse(ex.Message, HttpStatusCode.BadRequest);
            }
            catch (IncompatiblePermissionException ex)
            {
                Logger.Error(ex, ex.Message, parameters.roleId);
                return CreateFailureResponse(ex.Message, HttpStatusCode.BadRequest);
            }
        }

        private dynamic DeletePermissionsFromRole(dynamic parameters)
        {
            try
            {
                var roleApiModels = this.Bind<List<PermissionApiModel>>(new BindingConfig { BodyOnly = true });

                if (!Guid.TryParse(parameters.roleId, out Guid roleId))
                {
                    return CreateFailureResponse("roleId must be a guid.", HttpStatusCode.BadRequest);
                }

                if (roleApiModels.Count == 0)
                {
                    return CreateFailureResponse(
                        "No permissions specified to add, ensure an array of permissions is included in the request.",
                        HttpStatusCode.BadRequest);
                }

                Role roleToUpdate = _roleService.GetRole(roleId);
                CheckAccess(_clientService, roleToUpdate.Grain, roleToUpdate.SecurableItem, AuthorizationWriteClaim);
                Role updatedRole = _roleService.RemovePermissionsFromRole(roleToUpdate,
                    roleApiModels.Where(p => p.Id.HasValue).Select(p => p.Id.Value).ToArray());
                return CreateSuccessfulPostResponse(updatedRole.ToRoleApiModel(), HttpStatusCode.OK);
            }
            catch (RoleNotFoundException ex)
            {
                Logger.Error(ex, ex.Message, parameters.roleId);
                return CreateFailureResponse(ex.Message, HttpStatusCode.BadRequest);
            }
            catch (PermissionNotFoundException ex)
            {
                Logger.Error(ex, ex.Message, parameters.roleId);
                return CreateFailureResponse(ex.Message, HttpStatusCode.BadRequest);
            }
        }
    }
}
