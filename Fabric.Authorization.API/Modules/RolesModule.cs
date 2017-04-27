using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.Domain;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Roles;
using Nancy;
using Nancy.ModelBinding;

namespace Fabric.Authorization.API.Modules
{
    public class RolesModule : NancyModule
    {
        public RolesModule(IRoleService roleService) : base("/roles")
        {
            Get("/", parameters =>
            {
                var roles = roleService.GetRoles();
                return roles.Select(r => r.ToRoleApiModel());
            });

            Get("/{grain}", parameters =>
            {
                IEnumerable<Role> roles = roleService.GetRoles(parameters.grain);
                return roles.Select(r => r.ToRoleApiModel());
            });

            Get("/{grain}/{resource}", parameters =>
            {
                IEnumerable<Role> roles = roleService.GetRoles(parameters.grain, parameters.resource);
                return roles.Select(r => r.ToRoleApiModel());
            });

            Get("/{grain}/{resource}/{roleName}", parameters =>
            {
                IEnumerable<Role> roles = roleService.GetRoles(parameters.grain, parameters.resource, parameters.roleName);
                return roles.Select(r => r.ToRoleApiModel());
            });

            Post("/", parameters =>
            {
                try
                {
                    var roleApiModel = this.Bind<RoleApiModel>();
                    roleService.AddRole(roleApiModel.Grain, roleApiModel.Resource, roleApiModel.Name);
                    return HttpStatusCode.Created;
                }
                catch (RoleAlreadyExistsException)
                {
                    return HttpStatusCode.BadRequest;
                }
            });

            Delete("/{roleId}", parameters =>
            {
                try
                {
                    roleService.DeleteRole(parameters.roleId);
                    return HttpStatusCode.Created;
                }
                catch (RoleNotFoundException)
                {
                    return HttpStatusCode.BadRequest;
                }
            });
        }
    }
}
