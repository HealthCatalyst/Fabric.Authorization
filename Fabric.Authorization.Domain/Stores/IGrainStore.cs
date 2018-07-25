using System.Collections.Generic;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Stores
{
    public interface IGrainStore
    {
        Task<Grain> Get(string name);
        Task<IEnumerable<Grain>> GetSharedGrains();
        Task<IEnumerable<Grain>> GetAll();
    }
}