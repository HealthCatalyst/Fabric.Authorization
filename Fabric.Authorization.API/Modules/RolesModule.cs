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
            _roleService = roleService ?? throw new ArgumentNullException(nameof(roleService));
            _clientService = clientService ?? throw new ArgumentNullException(nameof(clientService));

            Get("/{grain}/{securableItem}", parameters =>
            {
                CheckAccess(_clientService, parameters.grain, parameters.securableItem, AuthorizationReadClaim);
                IEnumerable<Role> roles = roleService.GetRoles(parameters.grain, parameters.securableItem);
                return roles.Select(r => r.ToRoleApiModel());
            });

            Get("/{grain}/{securableItem}/{roleName}", parameters =>
            {
                CheckAccess(_clientService, parameters.grain, parameters.securableItem, AuthorizationReadClaim);
                IEnumerable<Role> roles = roleService.GetRoles(parameters.grain, parameters.securableItem, parameters.roleName);
                return roles.Select(r => r.ToRoleApiModel());
            });

            Post("/", parameters =>
            {
                var roleApiModel = this.Bind<RoleApiModel>(binderIgnore => binderIgnore.Id,
                    binderIgnore => binderIgnore.CreatedBy,
                    binderIgnore => binderIgnore.CreatedDateTimeUtc,
                    binderIgnore => binderIgnore.ModifiedDateTimeUtc,
                    binderIgnore => binderIgnore.ModifiedBy);

                var incomingRole = roleApiModel.ToRoleDomainModel();
                Validate(incomingRole);
                Role role = _roleService.AddRole(incomingRole);
                return CreateSuccessfulPostResponse(role.ToRoleApiModel());
            });

            Post("/{roleId}/permissions", parameters =>
            {
                try
                {
                    var roleApiModels = this.Bind<List<PermissionApiModel>>(new BindingConfig { BodyOnly = true });
                    roleService.AddPermissionsToRole(parameters.roleId, roleApiModels.Where(p => p.Id.HasValue).Select(p => p.Id.Value).ToArray());
                    return HttpStatusCode.NoContent;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            });

            Delete("/{roleId}", parameters =>
            {
                try
                {
                    roleService.DeleteRole(parameters.roleId);
                    return HttpStatusCode.NoContent;
                }
                catch (RoleNotFoundException)
                {
                    return HttpStatusCode.BadRequest;
                }
            });

            Delete("/{roleId}/permissions", parameters =>
            {
                try
                {
                    var roleApiModels = this.Bind<List<PermissionApiModel>>(new BindingConfig { BodyOnly = true });
                    roleService.RemovePermissionsFromRole(parameters.roleId, roleApiModels.Where(p => p.Id.HasValue).Select(p => p.Id.Value).ToArray());
                    return HttpStatusCode.NoContent;
                }
                catch (RoleNotFoundException)
                {
                    return HttpStatusCode.BadRequest;
                }
            });
        }
    }
}
