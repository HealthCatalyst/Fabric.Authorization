using System.Collections.Generic;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Stores
{
    public interface IGenericStore<K, T> where T : ITrackable
    {
        Task<T> Add(T model);

        Task<T> Get(K id);

        Task<IEnumerable<T>> GetAll();

        Task Delete(T model);

        Task Update(T model);

        Task BulkUpdate(IEnumerable<T> models, bool creation);

        Task<bool> Exists(K id);
    }
}