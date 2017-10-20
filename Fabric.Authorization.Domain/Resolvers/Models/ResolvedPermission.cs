using System;
using System.Collections.Generic;

namespace Fabric.Authorization.Domain.Resolvers.Models
{
    public class ResolvedPermission
    {
        public const string Allow = "allow";
        public const string Deny = "deny";

        public Guid Id { get; set; }
        public string Grain { get; set; }
        public string SecurableItem { get; set; }
        public string Name { get; set; }
        public string Action { get; set; }
        public ICollection<ResolvedPermissionRole> Roles { get; set; } = new List<ResolvedPermissionRole>();
        public DateTime CreatedDateTimeUtc { get; set; }
        public DateTime? ModifiedDateTimeUtc { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }

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

            var incomingPermission = obj as ResolvedPermission;

            return incomingPermission?.ToString().Equals(ToString(), StringComparison.OrdinalIgnoreCase) ?? false;
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }
    }
}