using System;

namespace Fabric.Authorization.Models
{
    public class PermissionRoleApiModel : IIdentifiable<Guid?>
    {
        public Guid? Id { get; set; }
        public string Name { get; set; }
    }
}