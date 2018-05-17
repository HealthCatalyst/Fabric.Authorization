using System;
using System.Collections.Generic;

namespace Fabric.Authorization.Domain.Models
{
    public class SecurableItem  : ITrackable, IIdentifiable<Guid>, ISoftDelete
    {
        public SecurableItem()
        {
            SecurableItems = new List<SecurableItem>();
        }

        public Guid Id { get; set; }

        public string Name { get; set; }

        public string ClientOwner { get; set; }

        public ICollection<SecurableItem> SecurableItems { get; set; }

        public string Grain { get; set; }

        public DateTime CreatedDateTimeUtc { get; set; }

        public DateTime? ModifiedDateTimeUtc { get; set; }

        public string CreatedBy { get; set; }

        public string ModifiedBy { get; set; }

        public bool IsDeleted { get; set; }
    }
}
