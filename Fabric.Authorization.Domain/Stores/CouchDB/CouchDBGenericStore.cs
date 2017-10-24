using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Services;
using Serilog;

namespace Fabric.Authorization.Domain.Stores.CouchDB
{
    public abstract class CouchDbGenericStore<K, T> : IGenericStore<K, T> where T : ITrackable, IIdentifiable
    {
        protected readonly IDocumentDbService DocumentDbService;
        private readonly IEventContextResolverService _eventContextResolverService;
        protected readonly ILogger Logger;
        protected readonly Stopwatch Stopwatch = new Stopwatch();

        protected CouchDbGenericStore(
            IDocumentDbService dbService,
            ILogger logger,
            IEventContextResolverService eventContextResolverService)
        {
            DocumentDbService = dbService;
            Logger = logger;
            _eventContextResolverService = eventContextResolverService ??
                                           throw new ArgumentNullException(nameof(eventContextResolverService));
        }

        public abstract Task<T> Add(T model);

        public virtual async Task Update(T model)
        {
            await Update(model.Identifier, model).ConfigureAwait(false);
        }

        protected virtual async Task Update(string id, T model)
        {
            model.Track(false, GetActor());
            await ExponentialBackoff(DocumentDbService.UpdateDocument(id, model)).ConfigureAwait(false);
        }

        public abstract Task Delete(T model);

        public virtual async Task<bool> Exists(K id)
        {
            var result = await DocumentDbService.GetDocument<T>(id.ToString()).ConfigureAwait(false);
            return result != null &&
                   (!(result is ISoftDelete) || !(result as ISoftDelete).IsDeleted);
        }

        public virtual async Task<T> Get(K id)
        {
            var result = await DocumentDbService.GetDocument<T>(id.ToString()).ConfigureAwait(false);
            if (result == null || result is ISoftDelete && (result as ISoftDelete).IsDeleted)
            {
                throw new NotFoundException<T>($"Could not find {typeof(T).Name} entity with ID {id}");
            }

            return result;
        }

        public virtual async Task<IEnumerable<T>> GetAll()
        {
            var documentType = $"{typeof(T).Name.ToLowerInvariant()}:";
            return await DocumentDbService.GetDocuments<T>(documentType);
        }

        public virtual async Task<T> Add(string id, T model)
        {
            model.Track(true, GetActor());
            await ExponentialBackoff(DocumentDbService.AddDocument(id, model)).ConfigureAwait(false);

            return model;
        }

        public virtual async Task Delete(string id, T model)
        {
            model.Track(false, GetActor());
            if (model is ISoftDelete)
            {
                (model as ISoftDelete).IsDeleted = true;
                await Update(model).ConfigureAwait(false);
            }
            else
            {
                Logger.Information($"Hard deleting {model.GetType()} {model.Identifier}");
                await DocumentDbService.DeleteDocument<T>(id).ConfigureAwait(false);
            }
        }

        protected string GetActor()
        {
            return _eventContextResolverService.Username ?? _eventContextResolverService.ClientId;
        }

        protected static async Task ExponentialBackoff(Task action, int maxRetries = 4, int wait = 100)
        {
            var retryCount = 1;

            while (retryCount <= maxRetries)
            {
                try
                {
                    await action;
                    break;
                }
                catch (Exception e) // TODO: Only retryable exceptions
                {
                    if (retryCount == maxRetries)
                    {
                        throw;
                    }
                    Console.WriteLine($"{e} Retrying {retryCount} ");
                    await Task.Delay(wait);
                    wait *= (int) Math.Pow(2, retryCount);
                    retryCount++;
                }
            }
        }
    }
}