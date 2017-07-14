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
    public class RolesModule : FabricModule<Role>
    {
        private readonly RoleService _roleService;
        private readonly ClientService _clientService;

        public RolesModule(RoleService roleService, ClientService clientService, RoleValidator validator, ILogger logger) : base("/roles", logger, validator)
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
            IEnumerable<Role> roles = _roleService.GetRoles(parameters.grain, parameters.securableItem).Result;
            return roles.Select(r => r.ToRoleApiModel());
        }

        private dynamic GetRoleByName(dynamic parameters)
        {
            CheckAccess(_clientService, parameters.grain, parameters.securableItem, AuthorizationReadClaim);
            IEnumerable<Role> roles = _roleService.GetRoles(parameters.grain, parameters.securableItem, parameters.roleName).Result;
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
            Role role = _roleService.AddRole(incomingRole).Result;
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

                var roleToDelete = _roleService.GetRole(roleId).Result;
                CheckAccess(_clientService, roleToDelete.Grain, roleToDelete.SecurableItem, AuthorizationWriteClaim);
                _roleService.DeleteRole(roleToDelete).Wait();
                return HttpStatusCode.NoContent;
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException is NotFoundException<Role>)
                {
                    Logger.Error(ex, ex.Message, parameters.roleId);
                    return CreateFailureResponse(ex.Message, HttpStatusCode.NotFound);
                }
                else
                {
                    throw;
                }
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

                Role roleToUpdate = _roleService.GetRole(roleId).Result;
                CheckAccess(_clientService, roleToUpdate.Grain, roleToUpdate.SecurableItem, AuthorizationWriteClaim);
                Role updatedRole = _roleService.AddPermissionsToRole(roleToUpdate,
                    roleApiModels.Where(p => p.Id.HasValue).Select(p => p.Id.Value).ToArray()).Result;
                return CreateSuccessfulPostResponse(updatedRole.ToRoleApiModel(), HttpStatusCode.OK);
            }
            catch (AggregateException ex)
            {
                Logger.Error(ex, ex.Message, parameters.roleId);
                if (ex.InnerException is NotFoundException<Role>)
                {
                    return CreateFailureResponse(ex.Message, HttpStatusCode.NotFound);
                }
                else if (ex.InnerException is NotFoundException<Permission>)
                {
                    return CreateFailureResponse(ex.Message, HttpStatusCode.NotFound);
                }
                else if (ex.InnerException is IncompatiblePermissionException)
                {
                    return CreateFailureResponse(ex.Message, HttpStatusCode.BadRequest);
                }
                else
                {
                    throw;
                }
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

                Role roleToUpdate = _roleService.GetRole(roleId).Result;
                CheckAccess(_clientService, roleToUpdate.Grain, roleToUpdate.SecurableItem, AuthorizationWriteClaim);
                Role updatedRole = _roleService.RemovePermissionsFromRole(roleToUpdate,
                    roleApiModels.Where(p => p.Id.HasValue).Select(p => p.Id.Value).ToArray()).Result;
                return CreateSuccessfulPostResponse(updatedRole.ToRoleApiModel(), HttpStatusCode.OK);
            }
            catch (AggregateException ex)
            {
                Logger.Error(ex, ex.Message, parameters.roleId);
                if (ex.InnerException is NotFoundException<Role>)
                {
                    return CreateFailureResponse(ex.Message, HttpStatusCode.NotFound);
                }
                else if (ex.InnerException is NotFoundException<Permission>)
                {
                    return CreateFailureResponse(ex.Message, HttpStatusCode.NotFound);
                }
                else
                {
                    throw;
                }
            }
        }
    }
}