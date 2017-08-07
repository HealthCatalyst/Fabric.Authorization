using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Stores
{
    public class InMemoryGenericStore<T> : IGenericStore<string, T> where T : ITrackable, IIdentifiable, ISoftDelete
    {
        protected readonly ConcurrentDictionary<string, T> _dictionary = new ConcurrentDictionary<string, T>();

        public async Task<T> Get(string id)
        {
            if (await Exists(id) && !_dictionary[id].IsDeleted)
            {
                return _dictionary[id];
            }

            throw new NotFoundException<T>(id.ToString());
        }

        public async virtual Task<T> Add(T model)
        {
            model.Track(creation: true);

            if (await Exists(model.Identifier))
            {
                throw new AlreadyExistsException<T>(model, model.Identifier);
            }

            _dictionary.TryAdd(model.Identifier, model);
            return model;
        }

        public async Task Delete(T model)
        {
            model.IsDeleted = true;
            await Update(model);
        }

        public async Task Update(T model)
        {
            model.Track();

            if (await this.Exists(model.Identifier))
            {
                if (!_dictionary.TryUpdate(model.Identifier, model, _dictionary[model.Identifier]))
                {
                    throw new CouldNotCompleteOperationException();
                }
            }
            else
            {
                throw new NotFoundException<T>(model, model.Identifier.ToString());
            }
        }

        public Task<bool> Exists(string id) => Task.FromResult(_dictionary.ContainsKey(id));

        public Task<IEnumerable<T>> GetAll() => Task.FromResult(_dictionary.Values.Where(g => !g.IsDeleted));
    }
}