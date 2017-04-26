using System.Collections.Generic;

namespace Fabric.Authorization.Domain
{
    public class Role
    {
        public string Name { get; set; }

        public string Grain { get; set; }

        public string Resource { get; set; }
        
        public IEnumerable<Permission> Permissions { get; set; }

        public override string ToString()
        {
            return $"{Grain}/{Resource}.{Name}";
        }
    }
}
