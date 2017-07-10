using System;
using System.Collections.Generic;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.API.Models
{
    public class SecurableItemApiModel : IIdentifiable
    {
        public Guid? Id { get; set; }
        public string Name { get; set; }
        public ICollection<SecurableItemApiModel> SecurableItems { get; set; }
        public string Identifier => Id.HasValue ? Id.ToString() : string.Empty;
        public DateTime CreatedDateTimeUtc { get; set; }
        public DateTime? ModifiedDateTimeUtc { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
    }
}
