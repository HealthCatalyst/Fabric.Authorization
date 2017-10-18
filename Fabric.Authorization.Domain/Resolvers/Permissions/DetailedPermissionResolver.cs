using System.Collections.Generic;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores.Services;

namespace Fabric.Authorization.Domain.Resolvers.Permissions
{
    public class DetailedPermissionResolver : IPermissionResolver
    {
        protected readonly GroupService GroupService;
        protected readonly RoleService RoleService;
        protected readonly UserService UserService;

        public DetailedPermissionResolver(
            UserService userService,
            GroupService groupService,
            RoleService roleService)
        {
            UserService = userService;
            GroupService = groupService;
            RoleService = roleService;
        }

        protected IEnumerable<Permission> AllowedPermissions => new List<Permission>();

        protected IEnumerable<Permission> DeniedPermissions => new List<Permission>();

        public virtual IEnumerable<Permission> Resolve(PermissionResolutionRequest resolutionRequest)
        {
            return new List<Permission>();
        }
    }
}