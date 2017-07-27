using System.Collections.Generic;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Serilog;

namespace Fabric.Authorization.Domain.Stores.CouchDB
{
    public abstract class CouchDBGenericStore<K, T> : IGenericStore<K, T> where T : ITrackable, IIdentifiable
    {
        protected readonly IDocumentDbService DbService;
        protected readonly ILogger Logger;

        protected CouchDBGenericStore(IDocumentDbService dbService, ILogger logger)
        {
            DbService = dbService;
            Logger = logger;
            this.AddViews();
        }

        public abstract Task<T> Add(T model);

        public virtual async Task<T> Add(string id, T model)
        {
            model.Track(creation: true);
            await DbService.AddDocument<T>(id, model).ConfigureAwait(false);

            return model;
        }

        public virtual async Task Update(T model)
        {
            await DbService.UpdateDocument<T>(model.Identifier, model).ConfigureAwait(false);
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
                Logger.Information($"Hard deleting {model.GetType()} {model.Identifier}");
                await DbService.DeleteDocument<T>(id).ConfigureAwait(false);
            }
        }

        public virtual async Task<bool> Exists(K id)
        {
            var result = await DbService.GetDocument<T>(id.ToString()).ConfigureAwait(false);
            return (result != null && 
                    (!(result is ISoftDelete) || !(result as ISoftDelete).IsDeleted));
        }

        public virtual async Task<T> Get(K id)
        {
            var result = await DbService.GetDocument<T>(id.ToString()).ConfigureAwait(false);
            if (result == null || ((result is ISoftDelete) && (result as ISoftDelete).IsDeleted))
            {
                throw new NotFoundException<T>();
            }

            return result;
        }

        public virtual async Task<IEnumerable<T>> GetAll()
        {
            var documentType = $"{typeof(T).Name.ToLowerInvariant()}:";
            return await DbService.GetDocuments<T>(documentType);
        }

        /// <summary>
        ///
        /// </summary>
        protected virtual async Task AddViews()
        {
            
        }
    }
}