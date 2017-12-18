using System.Collections.Generic;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Stores
{
    public interface IGenericStore<in TKey, TEntity>
        where TEntity : ITrackable
    {
        Task<TEntity> Get(TKey id);

        Task<TEntity> Add(TEntity model);

        Task<IEnumerable<TEntity>> GetAll();

        Task Delete(TEntity model);

        Task Update(TEntity model);

        Task<bool> Exists(TKey id);
    }
}