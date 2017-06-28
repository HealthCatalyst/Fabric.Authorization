using System;
using System.Collections.Generic;
using System.Text;

namespace Fabric.Authorization.Domain.Models
{
    public class User : ITrackable
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public ICollection<Permission> DeniedPermissions { get; set; }
        public ICollection<Permission> Permissions { get; set; }
        public DateTime CreatedDateTimeUtc { get; set; }
        public DateTime? ModifiedDateTimeUtc { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
    }
}
