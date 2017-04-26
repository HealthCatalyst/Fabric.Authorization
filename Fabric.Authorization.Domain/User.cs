using System.Collections.Generic;
using System.Linq;

namespace Fabric.Authorization.Domain
{
    public class User
    {
        public string Id { get; set; }
        
        public IEnumerable<Permission> Permissions
        {
            get { return Roles.Where(r => r.Permissions != null).SelectMany(r => r.Permissions); }
        }

        public ICollection<Role> Roles { get; set; }
    }
}
