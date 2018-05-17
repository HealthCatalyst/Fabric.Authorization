using System;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.API.Models
{
    public class PermissionRoleApiModel : IIdentifiable<Guid?>
    {
        public Guid? Id { get; set; }
        public string Name { get; set; }
    }
}