using System;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.API.Models
{
    public class ClientApiModel : IIdentifiable, ITrackable
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public SecurableItemApiModel TopLevelSecurableItem { get; set; }
        public DateTime CreatedDateTimeUtc { get; set; }
        public DateTime? ModifiedDateTimeUtc { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
        public string Identifier => Id;
    }
}
