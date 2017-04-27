using System;

namespace Fabric.Authorization.API.Models
{
    public class PermissionApiModel
    {
        public Guid Id { get; set; }

        public string Grain { get; set; }

        public string Resource { get; set; }

        public string Name { get; set; }
    }
}