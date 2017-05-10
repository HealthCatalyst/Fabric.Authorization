using System;

namespace Fabric.Authorization.API.Models
{
    public class PermissionApiModel : IIdentifiable
    {
        public Guid? Id { get; set; }

        public string Grain { get; set; }

        public string Resource { get; set; }

        public string Name { get; set; }

        public DateTime CreatedDateTimeUtc { get; set; }

        public DateTime? ModifiedDateTimeUtc { get; set; }

        public string CreatedBy { get; set; }

        public string ModifiedBy { get; set; }

    }
}