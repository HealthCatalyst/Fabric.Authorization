using System;

namespace Fabric.Authorization.Domain.Models
{
    public class Permission : ITrackable, IIdentifiable, ISoftDelete
    {
        public Guid Id { get; set; }

        public string Grain { get; set; }

        public string SecurableItem { get; set; }

        public string Name { get; set; }

        public string Identifier => Id.ToString();

        public bool IsDeleted { get; set; }

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

            var incomingPermission = obj as Permission;

            return incomingPermission?.ToString().Equals(ToString(), StringComparison.OrdinalIgnoreCase) ?? false;
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }
    }
}