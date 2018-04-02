using System.Collections.Generic;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.API.Models
{
    public class GroupRoleApiModel : IIdentifiable
    {
        public string Id { get; set; }

        public string GroupName { get; set; }

        public string DisplayName { get; set; }

        public string Description { get; set; }

        public IEnumerable<RoleApiModel> Roles { get; set; }

        /// <summary>
        /// Group source (e.g., Custom, Windows, Google). For custom groups, use "Custom".
        /// </summary>
        public string GroupSource { get; set; }

        public string Identifier => Id;
    }
}