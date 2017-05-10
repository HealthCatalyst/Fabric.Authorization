using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.Domain;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Services;
using Nancy;
using Nancy.ModelBinding;

namespace Fabric.Authorization.API.Modules
{
    public class RolesModule : NancyModule
    {
        public RolesModule(IRoleService roleService) : base("/roles")
        {
            Get("/{grain}/{securableItem}", parameters =>
            {
                IEnumerable<Role> roles = roleService.GetRoles(parameters.grain, parameters.securableItem);
                return roles.Select(r => r.ToRoleApiModel());
            });

            Get("/{grain}/{securableItem}/{roleName}", parameters =>
            {
                IEnumerable<Role> roles = roleService.GetRoles(parameters.grain, parameters.securableItem, parameters.roleName);
                return roles.Select(r => r.ToRoleApiModel());
            });

            Post("/", parameters =>
            {
                try
                {
                    var roleApiModel = this.Bind<RoleApiModel>();
                    roleService.AddRole(roleApiModel.Grain, roleApiModel.SecurableItem, roleApiModel.Name);
                    return HttpStatusCode.Created;
                }
                catch (RoleAlreadyExistsException)
                {
                    return HttpStatusCode.BadRequest;
                }
            });

            Post("/{roleId}/permissions", parameters =>
            {
                try
                {
                    var roleApiModels = this.Bind<List<PermissionApiModel>>(new BindingConfig{BodyOnly = true});
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
