using System;

namespace Fabric.Authorization.Models
{
    public class PermissionApiModel : ITrackable, IIdentifiable<Guid?>
    {
        public Guid? Id { get; set; }

        public string Grain { get; set; }

        public string SecurableItem { get; set; }

        public string Name { get; set; }

        public DateTime CreatedDateTimeUtc { get; set; }

        public DateTime? ModifiedDateTimeUtc { get; set; }

        public string CreatedBy { get; set; }

        public string ModifiedBy { get; set; }

        public PermissionAction PermissionAction { get; set; }

        public override string ToString()
        {
            return $"{Grain}/{SecurableItem}.{Name}";
        }
    }
  
    public enum PermissionAction
    {
        Allow,
        Deny
    }
}