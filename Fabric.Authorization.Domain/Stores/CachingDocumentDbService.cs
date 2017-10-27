using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Models;
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

        public async Task<T> GetDocument<T>(string documentId) where T : IIdentifiable
        {
            var fullDocumentId = DocumentDbHelpers.GetFullDocumentId<T>(documentId);
            if (_cache.TryGetValue(fullDocumentId, out T document))
            {
                return document;
            }

            document = await _innerDocumentDbService.GetDocument<T>(documentId);
            if (document != null)
            {
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromSeconds(30));
                _cache.Set(fullDocumentId, document, cacheEntryOptions);
            }
            return document;
        }

        public async Task<IEnumerable<T>> GetDocuments<T>(string documentType) where T : IIdentifiable
        {
            return await _innerDocumentDbService.GetDocuments<T>(documentType);
        }

        public async Task<int> GetDocumentCount(string documentType)
        {
            return await _innerDocumentDbService.GetDocumentCount(documentType);
        }

        public async Task AddDocument<T>(string documentId, T documentObject) where T : IIdentifiable
        {
            await _innerDocumentDbService.AddDocument(documentId, documentObject);
        }

        public async Task UpdateDocument<T>(string documentId, T documentObject) where T : IIdentifiable
        {
            var fullDocumentId = DocumentDbHelpers.GetFullDocumentId<T>(documentId);
            RemoveFromCache(fullDocumentId);
            await _innerDocumentDbService.UpdateDocument(documentId, documentObject);
        }

        public async Task BulkUpdateDocuments<T>(IEnumerable<string> documentIds, IEnumerable<T> documents)
            where T : IIdentifiable
        {
            var documentIdList = documentIds.ToList();
            foreach (var documentId in documentIdList)
            {
                var fullDocumentId = DocumentDbHelpers.GetFullDocumentId<T>(documentId);
                RemoveFromCache(fullDocumentId);
            }

            await _innerDocumentDbService.BulkUpdateDocuments(documentIdList, documents);
        }

        private void RemoveFromCache(string fullDocumentId)
        {
            if (_cache.TryGetValue(fullDocumentId, out var document))
            {
                _cache.Remove(fullDocumentId);
            }
        }

        public async Task DeleteDocument<T>(string documentId) where T : IIdentifiable
        {
            var fullDocumentId = DocumentDbHelpers.GetFullDocumentId<T>(documentId);
            RemoveFromCache(fullDocumentId);
            await _innerDocumentDbService.DeleteDocument<T>(documentId);
        }

        public async Task AddViews(string documentId, CouchDbViews views)
        {
            await _innerDocumentDbService.AddViews(documentId, views);
        }

        public async Task<IEnumerable<T>> GetDocuments<T>(
            string designdoc,
            string viewName,
            Dictionary<string, object> customParams) where T : IIdentifiable
        {
            return await _innerDocumentDbService.GetDocuments<T>(designdoc, viewName, customParams);
        }

        public async Task<IEnumerable<T>> GetDocuments<T>(string designdoc, string viewName, string customParams)
            where T : IIdentifiable
        {
            return await _innerDocumentDbService.GetDocuments<T>(designdoc, viewName, customParams);
        }
    }
}
