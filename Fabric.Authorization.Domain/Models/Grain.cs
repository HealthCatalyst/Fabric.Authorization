using System;
using System.Collections.Generic;

namespace Fabric.Authorization.Domain.Models
{
    public class Grain : IIdentifiable, ITrackable, ISoftDelete
    {
        public Grain()
        {
            SecurableItems = new List<SecurableItem>();
        }

        public Guid Id { get; set; }
        
        public string Name { get; set; }

        public bool IsShared { get; set; }

        public ICollection<SecurableItem> SecurableItems { get; set; }

        public ICollection<string> RequiredWriteScopes { get; set; }

        public string Identifier => Id.ToString();
        public DateTime CreatedDateTimeUtc { get; set; }
        public DateTime? ModifiedDateTimeUtc { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
        public bool IsDeleted { get; set; }
    }
}
