using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Stores.InMemory
{
    public class InMemoryGenericStore<T> : IGenericStore<string, T> where T : ITrackable, IIdentifiable, ISoftDelete
    {
        protected readonly ConcurrentDictionary<string, T> Dictionary = new ConcurrentDictionary<string, T>();

        public async Task<T> Get(string id)
        {
            if (await Exists(id).ConfigureAwait(false) && !Dictionary[id].IsDeleted)
            {
                return Dictionary[id];
            }

            throw new NotFoundException<T>(id);
        }

        public virtual async Task<T> Add(T model)
        {
            model.Track(creation: true);

            if (await Exists(model.Identifier).ConfigureAwait(false))
            {
                throw new AlreadyExistsException<T>(model, model.Identifier);
            }

            Dictionary.TryAdd(model.Identifier, model);
            return model;
        }

        public async Task Delete(T model)
        {
            model.IsDeleted = true;
            await Update(model).ConfigureAwait(false);
        }

        public async Task Update(T model)
        {
            model.Track();

            if (await this.Exists(model.Identifier).ConfigureAwait(false))
            {
                if (!Dictionary.TryUpdate(model.Identifier, model, Dictionary[model.Identifier]))
                {
                    throw new CouldNotCompleteOperationException();
                }
            }
            else
            {
                throw new NotFoundException<T>(model, model.Identifier.ToString());
            }
        }

        public Task<bool> Exists(string id) => Task.FromResult(Dictionary.ContainsKey(id));

        public Task<IEnumerable<T>> GetAll() => Task.FromResult(Dictionary.Values.Where(g => !g.IsDeleted));
    }
}