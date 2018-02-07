using System.Collections.Concurrent;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;

namespace Fabric.Authorization.Persistence.InMemory.Stores
{
    public class InMemoryGrainStore : IGrainStore
    {
        private readonly ConcurrentDictionary<string, Grain> _grains = new ConcurrentDictionary<string, Grain>();
        public async Task<Grain> Get(string name)
        {
            if (await Exists(name).ConfigureAwait(false) && !_grains[name].IsDeleted)
            {
                return _grains[name];
            }

            return null;
        }

        public Task<bool> Exists(string name)
        {
            return Task.FromResult(_grains.ContainsKey(name));
        }
    }
}
