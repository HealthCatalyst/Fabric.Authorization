using System;
using System.Collections.Generic;

namespace Catalyst.Fabric.Authorization.Models
{
    public class GroupRoleApiModel : IIdentifiable<Guid?>
    {
        public Guid? Id { get; set; }

        public string GroupName { get; set; }

        public string IdentityProvider { get; set; }

        public string DisplayName { get; set; }

        public string Description { get; set; }

        public IEnumerable<RoleApiModel> Roles { get; set; }

        /// <summary>
        ///     Group source (e.g., Custom or Directory).
        /// </summary>
        public string GroupSource { get; set; }

        public string Tenant { get; set; }

        public IEnumerable<GroupRoleApiModel> Parents { get; set; }
        public IEnumerable<GroupRoleApiModel> Children { get; set; }
    }
}