using System;
using System.Collections.Generic;
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
        private readonly IEventContextResolverService _eventContextResolverService;
        private const string EntityUpdated = "updated";
        private const string EntityCreated = "created";

        public AuditingDocumentDbService(IEventService eventService, IDocumentDbService innerDocumentDbService, IEventContextResolverService eventContextResolverService)
        {
            _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
            _innerDocumentDbService = innerDocumentDbService ??
                                      throw new ArgumentNullException(nameof(innerDocumentDbService));
            _eventContextResolverService = eventContextResolverService ??
                                           throw new ArgumentNullException(nameof(eventContextResolverService));
        }

        public async Task<T> GetDocument<T>(string documentId)
        {
            return await _innerDocumentDbService.GetDocument<T>(documentId);
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
            SetTrackableFields(documentObject, EntityCreated);
            await _eventService.RaiseEventAsync(new EntityAuditEvent<T>(EventTypes.EntityCreatedEvent, documentId, documentObject)).ConfigureAwait(false);
            await _innerDocumentDbService.AddDocument(documentId, documentObject);
        }

        public async Task UpdateDocument<T>(string documentId, T documentObject)
        {
            SetTrackableFields(documentObject, EntityUpdated);
            await _eventService.RaiseEventAsync(new EntityAuditEvent<T>(EventTypes.EntityUpdatedEvent, documentId, documentObject));
            await _innerDocumentDbService.UpdateDocument(documentId, documentObject);
        }

        public async Task DeleteDocument<T>(string documentId)
        {
            await _eventService.RaiseEventAsync(new EntityAuditEvent<T>(EventTypes.EntityDeletedEvent, documentId))
                .ConfigureAwait(false);
            await _innerDocumentDbService.DeleteDocument<T>(documentId);
        }

        public async Task AddViews(string documentId, CouchDbViews views)
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

        private void SetTrackableFields<T>(T documentObject, string entityAction)
        {
            var entity = documentObject as ITrackable;
            if (entity == null) return;
            var user = _eventContextResolverService.Username ?? _eventContextResolverService.ClientId;
            if (entityAction == EntityCreated)
            {
                entity.CreatedBy = user;
                entity.CreatedDateTimeUtc = DateTime.UtcNow;
            }
            else if (entityAction == EntityUpdated)
            {
                entity.ModifiedBy = user;
                entity.ModifiedDateTimeUtc = DateTime.UtcNow;
            }
        }
    }
}
