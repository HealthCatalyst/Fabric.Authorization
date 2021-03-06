﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Fabric.Authorization.Persistence.CouchDb.Stores;

namespace Fabric.Authorization.Persistence.CouchDb.Services
{
    public interface IDocumentDbService
    {
        Task<T> GetDocument<T>(string documentId);

        Task<IEnumerable<T>> GetDocuments<T>(string documentType);

        Task<int> GetDocumentCount(string documentType);

        Task AddDocument<T>(string documentId, T documentObject);

        Task UpdateDocument<T>(string documentId, T documentObject);

        Task DeleteDocument<T>(string documentId);

        Task AddViews(string documentId, CouchDbViews views);

        Task<IEnumerable<T>> GetDocuments<T>(string designdoc, string viewName, Dictionary<string, object> customParams);

        Task<IEnumerable<T>> GetDocuments<T>(string designdoc, string viewName, string customParams);

        Task Initialize();

        Task SetupDefaultUser();
    }
}