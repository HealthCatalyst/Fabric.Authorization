using System;
using System.Collections.Generic;

namespace Fabric.Authorization.Domain.Models
{
    public class SecurableItem  : ITrackable
    {
        public SecurableItem()
        {
            SecurableItems = new List<SecurableItem>();
        }

        public Guid Id { get; set; }
        public string Name { get; set; }
        public ICollection<SecurableItem> SecurableItems { get; set; }
        public DateTime CreatedDateTimeUtc { get; set; }
        public DateTime? ModifiedDateTimeUtc { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
    }
}
