using System;
using System.Collections.Generic;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.API.Models
{
    public class UserApiModel : IIdentifiable
    {
        public Guid? Id { get; set; }
        public string Name { get; set; }
        public ICollection<PermissionApiModel> DeniedPermissions { get; set; }
        public ICollection<PermissionApiModel> Permissions { get; set; }
        public DateTime CreatedDateTimeUtc { get; set; }
        public DateTime? ModifiedDateTimeUtc { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
        public string Identifier => Id.HasValue ? Id.ToString() : "";
    }
}