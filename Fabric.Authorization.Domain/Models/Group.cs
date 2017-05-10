using System.Collections.Generic;

namespace Fabric.Authorization.Domain.Models
{
    public class Group
    {
        public Group()
        {
            Roles = new List<Role>();
        }

        public string Id { get; set; }

        public string Name { get; set; }

        public bool IsDeleted { get; set; }

        public ICollection<Role> Roles { get; set; }
    }
}
