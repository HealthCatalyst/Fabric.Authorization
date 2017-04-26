using System.Collections.Generic;

namespace Fabric.Authorization.API.Models
{
    public class RoleApiModel
    {
        public int Id { get; set; }
        public string Grain { get; set; }
        public string Resource { get; set; }
        public string Name { get; set; }

        public IEnumerable<PermissionApiModel> Permissions { get; set; }
    }
}