using System.Collections.Generic;
using System.Threading.Tasks;
using Fabric.Authorization.API.Services;

namespace Fabric.Authorization.Domain.Stores
{
    public interface IDocumentDbService
    {
        Task<T> GetDocument<T>(string documentId);

        Task<IEnumerable<T>> GetDocuments<T>(string documentType);

        Task<int> GetDocumentCount(string documentType);

        void AddDocument<T>(string documentId, T documentObject);

        void UpdateDocument<T>(string documentId, T documentObject);

        void DeleteDocument<T>(string documentId);

        void AddViews(string documentId, CouchDBViews views);

        Task<IEnumerable<T>> GetDocuments<T>(string designdoc, string viewName, Dictionary<string, object> customParams);

        Task<IEnumerable<T>> GetDocuments<T>(string designdoc, string viewName, string customParams);
    }
}