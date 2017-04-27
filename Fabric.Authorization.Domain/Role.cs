using System;
using System.Collections.Generic;

namespace Fabric.Authorization.Domain
{
    public class Role
    {
        public Role()
        {
            Permissions = new List<Permission>();
        }

        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Grain { get; set; }

        public string Resource { get; set; }

        public bool IsDeleted { get; set; }
        
        public IEnumerable<Permission> Permissions { get; set; }

        public override string ToString()
        {
            return $"{Grain}/{Resource}.{Name}";
        }
    }
}
