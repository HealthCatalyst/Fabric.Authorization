using System.Threading.Tasks;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;

namespace Fabric.Authorization.Persistence.CouchDb.Stores
{
    public class CouchDbGrainStore : IGrainStore
    {
        public Task<Grain> Get(string name)
        {
            //  this allows for backward compatibility with CouchDb backed versions.
            //  this implementation will be removed once we convert clients over to the sql server backed version.
            return Task.FromResult(default(Grain));
        }
    }
}
