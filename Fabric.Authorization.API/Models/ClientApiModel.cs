using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fabric.Authorization.API.Models
{
    public class ClientApiModel : IIdentifiable
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public SecurableItemApiModel TopLevelSecurableItem { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedDateTimeUtc { get; set; }
        public DateTime? ModifiedDateTimeUtc { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
        public string Identifier => Id;
    }
}
