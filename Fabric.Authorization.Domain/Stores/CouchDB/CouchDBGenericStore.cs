using System;
using System.Collections.Generic;
using System.Text;
using Fabric.Authorization.Domain.Models;
using Serilog;

namespace Fabric.Authorization.Domain.Stores
{
    public abstract class CouchDBGenericStore<K,T> : IGenericStore<K,T> where T : ITrackable
    {
        private readonly IDocumentDbService _dbService;
        private readonly ILogger _logger;

        protected CouchDBGenericStore(IDocumentDbService dbService, ILogger logger)
        {
            _dbService = dbService;
            _logger = logger;
        }

        public abstract T Add(T model);

        public virtual T Add(string id, T model)
        {
            model.Track(creation: true);
            _dbService.AddDocument<T>(id, model);
            return model;
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
            return _dbService.GetDocument<T>(id.ToString()).Result;
        }

        public virtual IEnumerable<T> GetAll()
        {
            return null;
        }
    }
}
