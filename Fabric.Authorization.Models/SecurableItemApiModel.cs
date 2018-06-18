using System;
using System.Collections.Generic;
using Fabric.Authorization.Models.Enums;

namespace Fabric.Authorization.Models
{
    public class SecurableItemApiModel : ITrackable, IIdentifiable<Guid?>
    {
        public Guid? Id { get; set; }
        public string Name { get; set; }
        public string ClientOwner { get; set; }
        public string Grain { get; set; } = AuthorizationEnum.AppGrain;
        public ICollection<SecurableItemApiModel> SecurableItems { get; set; }
        public DateTime CreatedDateTimeUtc { get; set; }
        public DateTime? ModifiedDateTimeUtc { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
    }
}
