using System;
using System.Collections.Generic;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Persistence.SqlServer.EntityModels
{
    public class SecurableItem : ITrackable, ISoftDelete
    {
        public int Id { get; set; }
        public Guid SecurableItemId { get; set; }
        public string Name { get; set; }

        public DateTime CreatedDateTimeUtc { get; set; }
        public DateTime? ModifiedDateTimeUtc { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
        public bool IsDeleted { get; set; }

        public Guid? ParentId { get; set; }
        public SecurableItem Parent { get; set; }
        public ICollection<SecurableItem> SecurableItems { get; set; } = new List<SecurableItem>();
        public Client Client { get; set; }
        public ICollection<Permission> Permissions { get; set; }
        public ICollection<Role> Roles { get; set; }
    }
}
