using System;
using System.Collections.Generic;

namespace Fabric.Authorization.Domain.Models
{
    public class Role : ITrackable, IIdentifiable, ISoftDelete
    {
        public Role()
        {
            Permissions = new List<Permission>();
            DeniedPermissions = new List<Permission>();
            ChildRoles = new List<Guid>();
            Groups = new List<Group>();
            Users = new List<User>();
        }

        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Grain { get; set; }

        public string SecurableItem { get; set; }

        public bool IsDeleted { get; set; }

        public Guid? ParentRole { get; set; }

        public ICollection<Group> Groups { get; set; }
        
        public ICollection<Guid> ChildRoles { get; set; }

        public ICollection<Permission> Permissions { get; set; }

        public ICollection<Permission> DeniedPermissions { get; set; }

        public ICollection<User> Users { get; set; }

        public DateTime CreatedDateTimeUtc { get; set; }

        public DateTime? ModifiedDateTimeUtc { get; set; }

        public string CreatedBy { get; set; }

        public string ModifiedBy { get; set; }

        public string Identifier => Id.ToString();

        public override string ToString()
        {
            return $"{Grain}/{SecurableItem}.{Name}";
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (this == obj)
            {
                return true;
            }

            var role = obj as Role;
            if (role == null)
            {
                return false;
            }

            return Id == role.Id;
        }

        public override int GetHashCode()
        {
            return Identifier.GetHashCode();
        }
    }
}
