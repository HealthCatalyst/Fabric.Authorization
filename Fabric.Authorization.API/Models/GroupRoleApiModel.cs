using System;
using System.Collections.Generic;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.API.Models
{
    public class GroupRoleApiModel : IIdentifiable
    {
        public string Id { get; set; }
        public string GroupName { get; set; }
        public IEnumerable<RoleApiModel> Roles { get; set; }
        public string Identifier => Id;
    }
}
