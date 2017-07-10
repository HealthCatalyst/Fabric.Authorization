using System;
using System.Collections.Generic;
using System.Linq;
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

        public abstract T Add(T model);

        public virtual T Add(string id, T model)
        {
            model.Track(creation: true);
            try
            {
                _dbService.AddDocument<T>(id, model);
            }
            catch (ArgumentException e)
            {
                throw new AlreadyExistsException<T>(model, "Object not found!");
            }

            return model;
        }

        public virtual void Update(T model)
        {
            _dbService.UpdateDocument<T>(model.Identifier, model);
        }

        public abstract void Delete(T model);

        public virtual void Delete(string id, T model)
        {
            model.Track();
            _dbService.DeleteDocument<T>(id);
        }

        public virtual bool Exists(K id)
        {
            return _dbService.GetDocument<T>(id.ToString()).Result != null;
        }

        public virtual T Get(K id)
        {
            var result = _dbService.GetDocument<T>(id.ToString()).Result;
            if (result == null)
            {
                throw new NotFoundException<T>();
            }

            return result;
        }

        public virtual IEnumerable<T> GetAll()
        {
            return Enumerable.Empty<T>();
        }

        protected virtual void AddViews()
        {
        }
    }
}