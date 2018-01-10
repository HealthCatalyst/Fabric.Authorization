using System;
using System.Collections.Generic;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Persistence.SqlServer.EntityModels
{
    public class Permission : ITrackable, ISoftDelete
    {
        public Permission()
        {
            RolePermissions = new List<RolePermission>();
            UserPermissions = new List<UserPermission>();
        }

        public int Id { get; set; }
        public Guid PermissionId { get; set; }
        public Guid SecurableItemId { get; set; }
        public string Grain { get; set; }
        public string Name { get; set; }

        public bool IsDeleted { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDateTimeUtc { get; set; }
        public DateTime? ModifiedDateTimeUtc { get; set; }
        public string ModifiedBy { get; set; }

        public SecurableItem SecurableItem { get; set; }
        public ICollection<RolePermission> RolePermissions { get; set; }
        public ICollection<UserPermission> UserPermissions { get; set; }
    }
}