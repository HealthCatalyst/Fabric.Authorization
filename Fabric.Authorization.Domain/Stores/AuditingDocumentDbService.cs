using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Fabric.Authorization.API.Services;
using Fabric.Authorization.Domain.Events;
using Fabric.Authorization.Domain.Services;

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

        public Task<T> GetDocument<T>(string documentId)
        {
            _eventService.RaiseEventAsync(new EntityAuditEvent<T>(EventTypes.EntityReadEvent, documentId)).ConfigureAwait(false);
            return _innerDocumentDbService.GetDocument<T>(documentId);
        }

        public Task<IEnumerable<T>> GetDocuments<T>(string documentType)
        {
            _eventService.RaiseEventAsync(new EntityAuditEvent<T>(EventTypes.EntityReadEvent, "all")).ConfigureAwait(false);
            return _innerDocumentDbService.GetDocuments<T>(documentType);
        }

        public Task<int> GetDocumentCount(string documentType)
        {
            return _innerDocumentDbService.GetDocumentCount(documentType);
        }

        public void AddDocument<T>(string documentId, T documentObject)
        {
            _innerDocumentDbService.AddDocument(documentId, documentObject);
            _eventService.RaiseEventAsync(new EntityAuditEvent<T>(EventTypes.EntityCreatedEvent, documentId, documentObject)).ConfigureAwait(false);
        }

        public void UpdateDocument<T>(string documentId, T documentObject)
        {
            _innerDocumentDbService.UpdateDocument(documentId, documentObject);
            _eventService.RaiseEventAsync(new EntityAuditEvent<T>(EventTypes.EntityUpdatedEvent, documentId, documentObject));
        }

        public void DeleteDocument<T>(string documentId)
        {
            _innerDocumentDbService.DeleteDocument<T>(documentId);
            _eventService.RaiseEventAsync(new EntityAuditEvent<T>(EventTypes.EntityDeletedEvent, documentId))
                .ConfigureAwait(false);
        }

        public void AddViews(string documentId, CouchDBViews views)
        {
            _innerDocumentDbService.AddViews(documentId, views);
        }

        public Task<IEnumerable<T>> GetDocuments<T>(string designdoc, string viewName, Dictionary<string, object> customParams)
        {
            return _innerDocumentDbService.GetDocuments<T>(designdoc, viewName, customParams);
        }

        public Task<IEnumerable<T>> GetDocuments<T>(string designdoc, string viewName, string customParams)
        {
            return _innerDocumentDbService.GetDocuments<T>(designdoc, viewName, customParams);
        }
    }
}
