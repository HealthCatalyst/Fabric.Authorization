using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Events;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Domain.Stores.CouchDB;

namespace Fabric.Authorization.Domain.Stores
{
    public class AuditingDocumentDbService : IDocumentDbService
    {
        private readonly IEventService _eventService;
        private readonly IDocumentDbService _innerDocumentDbService;

        public AuditingDocumentDbService(IEventService eventService, IDocumentDbService innerDocumentDbService)
        {
            _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
            _innerDocumentDbService = innerDocumentDbService ??
                                      throw new ArgumentNullException(nameof(innerDocumentDbService));
        }

        public async Task<T> GetDocument<T>(string documentId) where T : IIdentifiable
        {
            return await _innerDocumentDbService.GetDocument<T>(documentId);
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
            await _eventService
                .RaiseEventAsync(new EntityAuditEvent<T>(EventTypes.EntityCreatedEvent, documentId, documentObject))
                .ConfigureAwait(false);
            await _innerDocumentDbService.AddDocument(documentId, documentObject);
        }

        public async Task UpdateDocument<T>(string documentId, T documentObject) where T : IIdentifiable
        {
            await _eventService.RaiseEventAsync(
                new EntityAuditEvent<T>(EventTypes.EntityUpdatedEvent, documentId, documentObject));
            await _innerDocumentDbService.UpdateDocument(documentId, documentObject);
        }

        public async Task BulkUpdateDocuments<T>(IEnumerable<string> documentIds, IEnumerable<T> documents)
            where T : IIdentifiable
        {
            var documentIdList = documentIds.ToList();
            var documentObjectList = documents.ToList();

            foreach (var documentId in documentIdList)
            {
                var documentObject = documentObjectList.Single(doc => doc.Identifier == documentId);
                await _eventService.RaiseEventAsync(
                    new EntityAuditEvent<T>(EventTypes.EntityUpdatedEvent, documentId, documentObject));
            }

            await _innerDocumentDbService.BulkUpdateDocuments(documentIdList, documentObjectList);
        }

        public async Task DeleteDocument<T>(string documentId) where T : IIdentifiable
        {
            await _eventService.RaiseEventAsync(new EntityAuditEvent<T>(EventTypes.EntityDeletedEvent, documentId))
                .ConfigureAwait(false);
            await _innerDocumentDbService.DeleteDocument<T>(documentId);
        }

        public async Task AddViews(string documentId, CouchDbViews views)
        {
            await _innerDocumentDbService.AddViews(documentId, views);
        }

        public async Task<IEnumerable<T>> GetDocuments<T>(string designdoc, string viewName,
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
