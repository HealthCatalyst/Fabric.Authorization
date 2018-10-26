using System;

namespace Catalyst.Fabric.Authorization.Models
{
    public class GroupRoleRequest
    {
        public string GroupName { get; set; }
        public string TenantId { get; set; }
        public string IdentityProvider { get; set; }

        public Guid? RoleId { get; set; }

        /// <summary>
        /// Role ID (for backwards compatibility)
        /// </summary>
        public Guid? Id { get; set; }

        public string Grain { get; set; }

        public string SecurableItem { get; set; }
    }
}
