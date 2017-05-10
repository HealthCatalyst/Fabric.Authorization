using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fabric.Authorization.API.Models
{
    public class SecurableItemApiModel
    {
        public Guid? Id { get; set; }
        public string Name { get; set; }
        public ICollection<SecurableItemApiModel> SecurableItems { get; set; }
    }
}
