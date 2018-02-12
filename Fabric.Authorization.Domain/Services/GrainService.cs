using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;

namespace Fabric.Authorization.Domain.Services
{
    public class GrainService
    {
        private readonly IGrainStore _grainStore;

        public GrainService(IGrainStore grainStore)
        {
            _grainStore = grainStore ?? throw new ArgumentNullException(nameof(grainStore));
        }

        public async Task<Grain> GetGrain(string name)
        {
            return await _grainStore.Get(name);
        }

        public async Task<IEnumerable<Grain>> GetSharedGrains()
        {
            return await _grainStore.GetSharedGrains();
        }
    }
}
