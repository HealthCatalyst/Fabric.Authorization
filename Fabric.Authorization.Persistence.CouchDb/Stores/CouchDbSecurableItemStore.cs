using System;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;

namespace Fabric.Authorization.Persistence.CouchDb.Stores
{
    public class CouchDbSecurableItemStore : ISecurableItemStore
    {
        public Task<SecurableItem> Get(string name)
        {
            var item = new SecurableItem
            {
                Name = name,
                ClientOwner = string.Empty
            };
            return Task.FromResult(item);
        }

        public Task<SecurableItem> Get(Guid id)
        {
            throw new NotImplementedException();
        }
    }
}
