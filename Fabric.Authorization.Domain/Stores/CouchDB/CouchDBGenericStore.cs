using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Serilog;

namespace Fabric.Authorization.Domain.Stores
{
    public abstract class CouchDBGenericStore<K, T> : IGenericStore<K, T> where T : ITrackable, IIdentifiable
    {
        protected readonly IDocumentDbService _dbService;
        protected readonly ILogger _logger;

        protected CouchDBGenericStore(IDocumentDbService dbService, ILogger logger)
        {
            _dbService = dbService;
            _logger = logger;
            this.AddViews();
        }

        public abstract Task<T> Add(T model);

        public virtual async Task<T> Add(string id, T model)
        {
            model.Track(creation: true);
            await _dbService.AddDocument<T>(id, model).ConfigureAwait(false);

            return model;
        }

        public virtual async Task Update(T model)
        {
            await _dbService.UpdateDocument<T>(model.Identifier, model).ConfigureAwait(false);
        }

        public abstract Task Delete(T model);

        public virtual async Task Delete(string id, T model)
        {
            model.Track();
            if (model is ISoftDelete)
            {
                (model as ISoftDelete).IsDeleted = true;
                await this.Update(model).ConfigureAwait(false);
            }
            else
            {
                _logger.Information($"Hard deleting {model.GetType()} {model.Identifier}");
                await _dbService.DeleteDocument<T>(id).ConfigureAwait(false);
            }
        }

        public virtual async Task<bool> Exists(K id)
        {
            var result = await _dbService.GetDocument<T>(id.ToString()).ConfigureAwait(false);
            return (result != null && 
                    (!(result is ISoftDelete) || !(result as ISoftDelete).IsDeleted));
        }

        public virtual async Task<T> Get(K id)
        {
            var result = await _dbService.GetDocument<T>(id.ToString()).ConfigureAwait(false);
            if (result == null || ((result is ISoftDelete) && (result as ISoftDelete).IsDeleted))
            {
                throw new NotFoundException<T>();
            }

            return result;
        }

        public virtual Task<IEnumerable<T>> GetAll() => Task.FromResult(Enumerable.Empty<T>());

        /// <summary>
        ///
        /// </summary>
        protected virtual void AddViews()
        {
        }
    }
}