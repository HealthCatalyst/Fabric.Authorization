using System.Collections.Generic;
using System.Linq;

namespace Fabric.Authorization.Domain
{
    public class User
    {
        public string Id { get; set; }

        public IEnumerable<Permission> Permissions
        {
            get { return Roles.SelectMany(r => r.Permissions); }
        }

        public IEnumerable<Role> Roles { get; set; }
    }
}
