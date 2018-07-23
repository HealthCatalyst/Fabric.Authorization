using System;

namespace Catalyst.Fabric.Authorization.Models.Requests
{
    public class RoleIdentifierApiRequest
    {
        public Guid RoleId { get; set; }
    }

    public class RolePatchApiRequest
    {
        public string DisplayName { get; set; }
        public string Description { get; set; }
    }
}
