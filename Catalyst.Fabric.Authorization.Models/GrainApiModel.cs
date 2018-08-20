using System;
using System.Collections.Generic;

namespace Catalyst.Fabric.Authorization.Models
{
    public class GrainApiModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public List<SecurableItemApiModel> SecurableItems { get; set; }
        public DateTime CreatedDateTimeUtc { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ModifiedDateTimeUtc { get; set; }
        public string ModifiedBy { get; set; }
        public ICollection<string> RequiredWriteScopes { get; set; }
        public bool IsShared { get; set; }
    }
}