using System;
using System.Collections.Generic;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Persistence.SqlServer.EntityModels
{
    public class Grain : ITrackable, ISoftDelete
    {
        public int Id { get; set; }
        public Guid GrainId { get; set; }
        public string Name { get; set; }
        public bool IsShared { get; set; }
        public string RequiredWriteScopes { get; set; }
        public ICollection<SecurableItem> SecurableItems { get; set; } = new List<SecurableItem>();

        public DateTime CreatedDateTimeUtc { get; set; }
        public DateTime? ModifiedDateTimeUtc { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
        public bool IsDeleted { get; set; }
    }
}
