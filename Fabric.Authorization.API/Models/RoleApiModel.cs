using System;
using System.Collections.Generic;

namespace Fabric.Authorization.API.Models
{
    public class RoleApiModel
    {
        public Guid? Id { get; set; }
        public string Grain { get; set; }
        public string SecurableItem { get; set; }
        public string Name { get; set; }

        public IEnumerable<PermissionApiModel> Permissions { get; set; }
    }
}