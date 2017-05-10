using System;

namespace Fabric.Authorization.Domain.Models
{
    public class Permission : ITrackable
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

        public override string ToString()
        {
            return $"{Grain}/{SecurableItem}.{Name}";
        }
    }
}
