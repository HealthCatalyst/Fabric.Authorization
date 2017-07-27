using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Stores.CouchDB;
using Microsoft.Extensions.Caching.Memory;

namespace Fabric.Authorization.Domain.Stores
{
    public class CachingDocumentDbService : IDocumentDbService
    {
        private readonly IDocumentDbService _innerDocumentDbService;
        private readonly IMemoryCache _cache;

        public CachingDocumentDbService(IDocumentDbService innerDocumentDbService, IMemoryCache memoryCache)
        {
            _innerDocumentDbService = innerDocumentDbService ??
                                      throw new ArgumentNullException(nameof(innerDocumentDbService));
            _cache = memoryCache;
        }
        public async Task<T> GetDocument<T>(string documentId)
        {
            T document;
            var fullDocumentId = DocumentDbHelpers.GetFullDocumentId<T>(documentId);
            if (!_cache.TryGetValue(fullDocumentId, out document))
            {
                document = await _innerDocumentDbService.GetDocument<T>(documentId);
                if (document != null)
                {
                    var cacheEntryOptions = new MemoryCacheEntryOptions()
                        .SetSlidingExpiration(TimeSpan.FromSeconds(30));
                    _cache.Set(fullDocumentId, document, cacheEntryOptions);
                }
            }
            return document;
        }

        public async Task<IEnumerable<T>> GetDocuments<T>(string documentType)
        {
            return await _innerDocumentDbService.GetDocuments<T>(documentType);
        }

        public async Task<int> GetDocumentCount(string documentType)
        {
            return await _innerDocumentDbService.GetDocumentCount(documentType);
        }

        public async Task AddDocument<T>(string documentId, T documentObject)
        {
            await _innerDocumentDbService.AddDocument(documentId, documentObject);
        }

        public async Task UpdateDocument<T>(string documentId, T documentObject)
        {
            var fullDocumentId = DocumentDbHelpers.GetFullDocumentId<T>(documentId);
            if (_cache.TryGetValue(fullDocumentId, out var document))
            {
                _cache.Remove(fullDocumentId);
            }
            await _innerDocumentDbService.UpdateDocument(documentId, documentObject);
        }

        public async Task DeleteDocument<T>(string documentId)
        {
            var fullDocumentId = DocumentDbHelpers.GetFullDocumentId<T>(documentId);
            if (_cache.TryGetValue(fullDocumentId, out var document))
            {
                _cache.Remove(fullDocumentId);
            }
            await _innerDocumentDbService.DeleteDocument<T>(documentId);
        }

        public async Task AddViews(string documentId, CouchDBViews views)
        {
            await _innerDocumentDbService.AddViews(documentId, views);
        }

        public async Task<IEnumerable<T>> GetDocuments<T>(string designdoc, string viewName, Dictionary<string, object> customParams)
        {
            return await _innerDocumentDbService.GetDocuments<T>(designdoc, viewName, customParams);
        }

        public async Task<IEnumerable<T>> GetDocuments<T>(string designdoc, string viewName, string customParams)
        {
            return await _innerDocumentDbService.GetDocuments<T>(designdoc, viewName, customParams);
        }
    }
}
