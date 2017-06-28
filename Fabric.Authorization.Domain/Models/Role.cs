using System;
using System.Collections.Generic;

namespace Fabric.Authorization.Domain.Models
{
    public class Role : ITrackable
    {
        public Role()
        {
            Permissions = new List<Permission>();
            DeniedPermissions = new List<Permission>();
            ChildRoles = new List<Role>();
        }

        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Grain { get; set; }

        public string SecurableItem { get; set; }

        public bool IsDeleted { get; set; }

        public Role ParentRole { get; set; }
        
        public ICollection<Role> ChildRoles { get; set; }

        public ICollection<Permission> Permissions { get; set; }

        public ICollection<Permission> DeniedPermissions { get; set; }

        public DateTime CreatedDateTimeUtc { get; set; }
        public DateTime? ModifiedDateTimeUtc { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }

        public override string ToString()
        {
            return $"{Grain}/{SecurableItem}.{Name}";
        }
    }
}
