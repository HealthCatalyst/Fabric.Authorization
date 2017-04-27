using System.Collections.Generic;
using System.Linq;

namespace Fabric.Authorization.Domain
{
    public class User
    {
        public User()
        {
            Roles = new List<Role>();
        }

        public string Id { get; set; }

        public bool IsDeleted { get; set; }
        
        public IEnumerable<Permission> Permissions
        {
            get { return Roles.Where(r => r.Permissions != null && !r.IsDeleted).SelectMany(r => r.Permissions.Where(p => !p.IsDeleted)); }
        }

        public ICollection<Role> Roles { get; set; }
    }
}
