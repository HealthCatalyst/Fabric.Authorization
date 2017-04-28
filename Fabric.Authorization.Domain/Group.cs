using System;
using System.Collections.Generic;
using System.Text;

namespace Fabric.Authorization.Domain
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
