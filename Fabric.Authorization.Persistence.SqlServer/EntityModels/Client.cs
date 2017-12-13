using System;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Persistence.SqlServer.EntityModels
{
    public class Client : ITrackable, ISoftDelete
    {
        public int Id { get; set; }
        public string ClientId { get; set; }
        public string Name { get; set; }
        public int SecurableItemId { get; set; }
        
        public DateTime CreatedDateTimeUtc { get; set; }
        public DateTime? ModifiedDateTimeUtc { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
        public bool IsDeleted { get; set; }

        public SecurableItem TopLevelSecurableItem { get; set; }
    }
}
