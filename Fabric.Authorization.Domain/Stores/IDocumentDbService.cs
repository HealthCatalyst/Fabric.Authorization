using System.Collections.Generic;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores.CouchDB;

namespace Fabric.Authorization.Domain.Stores
{
    public interface IDocumentDbService
    {
        Task<T> GetDocument<T>(string documentId) where T : IIdentifiable;

        Task<IEnumerable<T>> GetDocuments<T>(string documentType) where T : IIdentifiable;

        Task<int> GetDocumentCount(string documentType);

        Task AddDocument<T>(string documentId, T documentObject) where T : IIdentifiable;

        Task UpdateDocument<T>(string documentId, T documentObject) where T : IIdentifiable;

        Task BulkUpdateDocuments<T>(IEnumerable<string> documentIds, IEnumerable<T> documents) where T : IIdentifiable;

        Task DeleteDocument<T>(string documentId) where T : IIdentifiable;

        Task AddViews(string documentId, CouchDbViews views);

        Task<IEnumerable<T>> GetDocuments<T>(string designdoc, string viewName, Dictionary<string, object> customParams)
            where T : IIdentifiable;

        Task<IEnumerable<T>> GetDocuments<T>(string designdoc, string viewName, string customParams)
            where T : IIdentifiable;
    }
}