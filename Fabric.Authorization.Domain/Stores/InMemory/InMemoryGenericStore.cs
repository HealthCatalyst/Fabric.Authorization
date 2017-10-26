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

        public virtual async Task<T> Get(string id)
        {
            if (await Exists(id).ConfigureAwait(false) && !Dictionary[id].IsDeleted)
            {
                return Dictionary[id];
            }

            throw new NotFoundException<T>($"Could not find {typeof(T).Name} entity with ID {id}");
        }

        public virtual async Task<T> Add(T model)
        {
            model.Track();

            if (await Exists(model.Identifier).ConfigureAwait(false))
            {
                throw new AlreadyExistsException<T>(model, model.Identifier);
            }

            Dictionary.TryAdd(model.Identifier, model);
            return model;
        }

        public virtual async Task Delete(T model)
        {
            model.IsDeleted = true;
            await Update(model).ConfigureAwait(false);
        }

        public virtual async Task Update(T model)
        {
            model.Track();

            if (await Exists(model.Identifier).ConfigureAwait(false))
            {
                if (!Dictionary.TryUpdate(model.Identifier, model, Dictionary[model.Identifier]))
                {
                    throw new CouldNotCompleteOperationException();
                }
            }
            else
            {
                throw new NotFoundException<T>(model, model.Identifier);
            }
        }

        public virtual Task<bool> Exists(string id)
        {
            return Task.FromResult(Dictionary.ContainsKey(id));
        }

        public Task<IEnumerable<T>> GetAll()
        {
            return Task.FromResult(Dictionary.Values.Where(g => !g.IsDeleted));
        }
    }
}