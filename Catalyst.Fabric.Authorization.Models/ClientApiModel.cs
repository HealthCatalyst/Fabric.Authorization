using System;

namespace Catalyst.Fabric.Authorization.Models
{
    public class ClientApiModel : ITrackable, IIdentifiable<string>
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public SecurableItemApiModel TopLevelSecurableItem { get; set; }
        public DateTime CreatedDateTimeUtc { get; set; }
        public DateTime? ModifiedDateTimeUtc { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
    }
}
