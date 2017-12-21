using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Persistence.SqlServer.EntityModels
{
    public class Role : ITrackable, ISoftDelete
    {
        public Role()
        {
            GroupRoles = new List<GroupRole>();
            ChildRoles = new List<Role>();
            RolePermissions = new List<RolePermission>();
        }

        public int Id { get; set; }
        public Guid RoleId { get; set; }
        public Guid? ParentRoleId { get; set; }
        public int SecurableItemId { get; set; }
        public string Grain { get; set; }
        public string Name { get; set; }

        public bool IsDeleted { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDateTimeUtc { get; set; }
        public DateTime? ModifiedDateTimeUtc { get; set; }
        public string ModifiedBy { get; set; }

        public SecurableItem SecurableItem { get; set; }
        public Role ParentRole { get; set; }
        public ICollection<GroupRole> GroupRoles { get; set; }
        public ICollection<Role> ChildRoles { get; set; }
        public ICollection<RolePermission> RolePermissions { get; set; }

        [NotMapped]
        public ICollection<Group> Groups => GroupRoles.Select(gr => gr.Group).ToList();

        [NotMapped]
        public ICollection<Permission> AllowedPermissions =>
            RolePermissions
                .Where(rp => rp.PermissionAction == PermissionAction.Allow)
                .Select(rp => rp.Permission).ToList();

        [NotMapped]
        public ICollection<Permission> DeniedPermissions =>
            RolePermissions
                .Where(rp => rp.PermissionAction == PermissionAction.Deny)
                .Select(rp => rp.Permission).ToList();
    }
}