using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Services;
using Serilog;

namespace Fabric.Authorization.Domain.Stores.CouchDB
{
    public abstract class CouchDbGenericStore<K, T> : IGenericStore<K, T> where T : ITrackable, IIdentifiable
    {
        protected readonly IDocumentDbService _dbService;
        protected readonly ILogger _logger;
        private readonly IEventContextResolverService _eventContextResolverService;

        protected CouchDbGenericStore(IDocumentDbService dbService, ILogger logger, IEventContextResolverService eventContextResolverService)
        {
            _dbService = dbService;
            _logger = logger;
            _eventContextResolverService = eventContextResolverService ??
                                           throw new ArgumentNullException(nameof(eventContextResolverService));
        }

        public abstract Task<T> Add(T model);

        public virtual async Task<T> Add(string id, T model)
        {
            model.Track(creation: true, user: GetActor());
            await _dbService.AddDocument<T>(id, model).ConfigureAwait(false);

            return model;
        }

        public virtual async Task Update(T model)
        {
            model.Track(creation: false, user: GetActor());
            await _dbService.UpdateDocument<T>(model.Identifier, model).ConfigureAwait(false);
        }

        public abstract Task Delete(T model);

        public virtual async Task Delete(string id, T model)
        {
            model.Track(creation: false, user: GetActor());
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

        public virtual async Task<IEnumerable<T>> GetAll()
        {
            var documentType = $"{typeof(T).Name.ToLowerInvariant()}:";
            return await _dbService.GetDocuments<T>(documentType);
        }

        private string GetActor()
        {
            return _eventContextResolverService.Username ?? _eventContextResolverService.ClientId;
        }
    }
}