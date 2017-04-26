using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Fabric.Authorization.Domain;

namespace Fabric.Authorization.API.Models
{
    public static class ModelExtensions
    {
        public static RoleApiModel ToRoleApiModel(this Role role)
        {
            var roleApiModel = new RoleApiModel
            {
                Id = role.Id,
                Grain = role.Grain,
                Resource = role.Resource,
                Name = role.Name,
                Permissions = role.Permissions?.Select(p => p.ToPermissionApiModel())
            };
            return roleApiModel;
        }

        public static PermissionApiModel ToPermissionApiModel(this Permission permission)
        {
            var permissionApiModel = new PermissionApiModel
            {
                Id = permission.Id,
                Grain = permission.Grain,
                Name = permission.Name,
                Resource = permission.Resource
            };
            return permissionApiModel;
        }
    }
}
