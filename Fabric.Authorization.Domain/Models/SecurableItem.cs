using System;
using System.Collections.Generic;

namespace Fabric.Authorization.Domain.Models
{
    public class SecurableItem
    {
        public SecurableItem()
        {
            SecurableItems = new List<SecurableItem>();
        }

        public Guid Id { get; set; }
        public string Name { get; set; }
        public ICollection<SecurableItem> SecurableItems { get; set; }
    }
}
