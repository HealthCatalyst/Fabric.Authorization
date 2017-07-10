using System.Collections.Generic;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Stores
{
    public interface IGenericStore<K, T> where T : ITrackable
    {
        T Add(T model);

        T Get(K id);

        IEnumerable<T> GetAll();

        void Delete(T model);

        void Update(T model);

        bool Exists(K id);

    }
}