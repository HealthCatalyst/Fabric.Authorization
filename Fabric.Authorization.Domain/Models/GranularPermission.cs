using System;
using System.Collections.Generic;

namespace Fabric.Authorization.Domain.Models
{
    public class GranularPermission : ITrackable, IIdentifiable, ISoftDelete
    {
        public string Id => Target;

        public IEnumerable<Permission> DeniedPermissions { get; set; }

        public IEnumerable<Permission> AdditionalPermissions { get; set; }

        public string Target { get; set; }

        public string Identifier => Id;

        public bool IsDeleted { get; set; }

        public DateTime CreatedDateTimeUtc { get; set; }

        public DateTime? ModifiedDateTimeUtc { get; set; }

        public string CreatedBy { get; set; }

        public string ModifiedBy { get; set; }

        public override string ToString()
        {
            return $"{Target}";
        }
    }
}