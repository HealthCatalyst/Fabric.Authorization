using System;
using System.Collections.Generic;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.API.Models
{
    public class SecurableItemApiModel : ITrackable, IIdentifiable<Guid?>
    {
        public Guid? Id { get; set; }
        public string Name { get; set; }
        public string ClientOwner { get; set; }
        public string Grain { get; set; } = Domain.Defaults.Authorization.AppGrain;
        public ICollection<SecurableItemApiModel> SecurableItems { get; set; }
        public DateTime CreatedDateTimeUtc { get; set; }
        public DateTime? ModifiedDateTimeUtc { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
    }
}
