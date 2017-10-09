using System;

namespace Fabric.Authorization.Domain.Models
{
    public class Permission : ITrackable, IIdentifiable, ISoftDelete
    {
        public Guid Id { get; set; }

        public string Grain { get; set; }

        public string SecurableItem { get; set; }

        public string Name { get; set; }

        public bool IsDeleted { get; set; }

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
            if(obj == null)
            {
                return false;
            }

            var incomingPermission = obj as Permission;
            if(incomingPermission == null)
            {
                return false;
            }

            return incomingPermission.ToString().Equals(this.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }
    }
}
