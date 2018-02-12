using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;

namespace Fabric.Authorization.Persistence.InMemory.Stores
{
    public class InMemoryGrainStore : IGrainStore
    {
        private static readonly ConcurrentDictionary<string, Grain> Grains = new ConcurrentDictionary<string, Grain>();

        public InMemoryGrainStore(Domain.Defaults.Authorization authorizationDefaults)
        {
            foreach (var grain in authorizationDefaults.Grains)
            {
                Grains.TryAdd(grain.Name, grain);
            }
        }

        public async Task<Grain> Get(string name)
        {
            if (await Exists(name).ConfigureAwait(false) && !Grains[name].IsDeleted)
            {
                return Grains[name];
            }

            throw new NotFoundException<Grain>($"Could not find {typeof(Grain).Name} entity with Name {name}");
        }

        public Task<IEnumerable<Grain>> GetSharedGrains()
        {
            return Task.FromResult(Grains.Where(g => g.Value.IsShared).Select(g => g.Value));
        }

        public Task<bool> Exists(string name)
        {
            return Task.FromResult(Grains.ContainsKey(name));
        }
    }
}
