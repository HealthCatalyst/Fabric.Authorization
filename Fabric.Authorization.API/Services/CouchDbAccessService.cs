using System.Collections.Generic;
using System.Threading.Tasks;
using MyCouch;
using MyCouch.Net;
using MyCouch.Requests;
using MyCouch.Responses;
using Newtonsoft.Json;
using Serilog;
using System;
using Fabric.Authorization.API.Configuration;
using Fabric.Authorization.Domain.Stores;
using System.Linq;

namespace Fabric.Authorization.API.Services
{
    public class CouchDbAccessService : IDocumentDbService
    {
        private readonly ILogger _logger;
        private readonly ICouchDbSettings _couchDbSettings;
        private const string HighestUnicodeChar = "\ufff0";

        private string GetFullDocumentId<T>(string documentId)
        {
            return $"{typeof(T).Name.ToLowerInvariant()}:{documentId}";
        }

        private DbConnectionInfo DbConnectionInfo
        {
            get
            {
                var connectionInfo = new DbConnectionInfo(_couchDbSettings.Server, _couchDbSettings.DatabaseName);

                if (!string.IsNullOrEmpty(_couchDbSettings.Username) &&
                    !string.IsNullOrEmpty(_couchDbSettings.Password))
                {
                    connectionInfo.BasicAuth =
                        new BasicAuthString(_couchDbSettings.Username, _couchDbSettings.Password);
                }

                return connectionInfo;
            }
        }

        public CouchDbAccessService(ICouchDbSettings config, ILogger logger)
        {
            _couchDbSettings = config;
            _logger = logger;

            _logger.Debug(
                $"couchDb configuration properties: Server: {config.Server} -- DatabaseName: {config.DatabaseName}");

            using (var client = new MyCouchClient(DbConnectionInfo))
            {
                if (!client.Database.GetAsync().Result.IsSuccess)
                {
                    var creation = client.Database.PutAsync().Result;
                    if (!creation.IsSuccess)
                    {
                        throw new ArgumentException(creation.Error);
                    }
                }
            }

        }

            public Task<T> GetDocument<T>(string documentId)
        {

            using (var client = new MyCouchClient(DbConnectionInfo))
            {
                var documentResponse = client.Documents.GetAsync(GetFullDocumentId<T>(documentId)).Result;

                if (!documentResponse.IsSuccess)
                {
                    _logger.Debug($"unable to find document: {GetFullDocumentId<T>(documentId)} - message: {documentResponse.Reason}");
                    return Task.FromResult(default(T));
                }

                var json = documentResponse.Content;
                var document = JsonConvert.DeserializeObject<T>(json);

                return Task.FromResult(document);
            }
        }

        public Task<IEnumerable<T>> GetDocuments<T>(string documentType)
        {
            using (var client = new MyCouchClient(DbConnectionInfo))
            {
                var viewQuery = new QueryViewRequest(SystemViewIdentity.AllDocs)
                    .Configure(q => q.Reduce(false)
                        .IncludeDocs(true)
                        .StartKey(documentType)
                        .EndKey($"{documentType}{HighestUnicodeChar}"));

                ViewQueryResponse result = client.Views.QueryAsync(viewQuery).Result;

                if (!result.IsSuccess)
                {
                    _logger.Error($"unable to find documents for type: {documentType} - error: {result.Reason}");
                    return Task.FromResult(default(IEnumerable<T>));
                }

                var results = new List<T>();

                foreach (var responseRow in result.Rows)
                {
                    var resultRow = JsonConvert.DeserializeObject<T>(responseRow.IncludedDoc);
                    results.Add(resultRow);
                }

                return Task.FromResult((IEnumerable<T>)results);
            }
        }

        public Task<int> GetDocumentCount(string documentType)
        {
            using (var client = new MyCouchClient(DbConnectionInfo))
            {
                var viewQuery = new QueryViewRequest("TODO",
                    documentType);
                var result = client.Views.QueryAsync<int>(viewQuery).Result;
                if (result.Rows != null && result.Rows.Length > 0)
                {
                    return Task.FromResult(result.Rows[0].Value);
                }
                return Task.FromResult(0);
            }
        }

        public void AddDocument<T>(string documentId, T documentObject)
        {
            var fullDocumentId = GetFullDocumentId<T>(documentId);

            using (var client = new MyCouchClient(DbConnectionInfo))
            {
                var existingDoc = client.Documents.GetAsync(fullDocumentId).Result;
                var docJson = JsonConvert.SerializeObject(documentObject);

                if (!string.IsNullOrEmpty(existingDoc.Id))
                {
                    throw new ArgumentException($"Document with id {documentId} already exists.");
                }

                var response = client.Documents.PutAsync(fullDocumentId, docJson).Result;

                if (!response.IsSuccess)
                {
                    _logger.Error($"unable to add or update document: {documentId} - error: {response.Reason}");
                }
            }
        }

        public void UpdateDocument<T>(string documentId, T documentObject)
        {
            var fullDocumentId = GetFullDocumentId<T>(documentId);

            using (var client = new MyCouchClient(DbConnectionInfo))
            {
                var existingDoc = client.Documents.GetAsync(fullDocumentId).Result;
                var docJson = JsonConvert.SerializeObject(documentObject);

                if (existingDoc.IsEmpty)
                {
                    throw new ArgumentException($"Document with id {documentId} does not exist.");
                }

                var response = client.Documents.PutAsync(fullDocumentId, existingDoc.Rev, docJson).Result;

                if (!response.IsSuccess)
                {
                    _logger.Error($"unable to add or update document: {documentId} - error: {response.Reason}");
                }
            }
        }

        public void DeleteDocument<T>(string documentId)
        {
            using (var client = new MyCouchClient(DbConnectionInfo))
            {
                var documentResponse = client.Documents.GetAsync(GetFullDocumentId<T>(documentId)).Result;

                Delete(documentResponse.Id, documentResponse.Rev);
            }
        }

        private void Delete(string documentId, string rev)
        {
            using (var client = new MyCouchClient(DbConnectionInfo))
            {
                var response = client.Documents.DeleteAsync(documentId, rev).Result;

                if (!response.IsSuccess)
                {
                    _logger.Error($"There was an error deleting document:{documentId}, error: {response.Reason}");
                }
            }
        }

        public void AddViews(string documentId, CouchDBViews views)
        {
            var fullDocumentId = $"_design/{documentId}";

            using (var client = new MyCouchClient(DbConnectionInfo))
            {
                var existingDoc = client.Documents.GetAsync(fullDocumentId).Result;
                var docJson = JsonConvert.SerializeObject(views);

                if (!string.IsNullOrEmpty(existingDoc.Id))
                {
                    return;
                }

                var response = client.Documents.PutAsync(fullDocumentId, docJson).Result;

                if (!response.IsSuccess)
                {
                    _logger.Error($"unable to add or update document: {documentId} - error: {response.Reason}");
                }
            }
        }

        public Task<IEnumerable<T>> GetDocuments<T>(string designdoc, string viewName, Dictionary<string, object> customParams)
        {
            using (var client = new MyCouchClient(DbConnectionInfo))
            {
                var viewQuery = new QueryViewRequest(designdoc, viewName);
                viewQuery.CustomQueryParameters = customParams;
                ViewQueryResponse result = client.Views.QueryAsync(viewQuery).Result;

                if (!result.IsSuccess)
                {
                    _logger.Error($"unable to execute view: {viewName} - error: {result.Reason}");
                    return Task.FromResult(default(IEnumerable<T>));
                }

                var results = new List<T>();

                foreach (var responseRow in result.Rows)
                {
                    var resultRow = JsonConvert.DeserializeObject<T>(responseRow.IncludedDoc);
                    results.Add(resultRow);
                }

                return Task.FromResult((IEnumerable<T>)results);
            }
        }
        
        public Task<IEnumerable<T>> GetDocuments<T>(string designdoc, string viewName, string key)
        {
            using (var client = new MyCouchClient(DbConnectionInfo))
            {
                var viewQuery = new QueryViewRequest(designdoc, viewName);
                ViewQueryResponse result = client.Views.QueryAsync(viewQuery).Result;

                if (!result.IsSuccess)
                {
                    _logger.Error($"unable to execute view: {viewName} - error: {result.Reason}");
                    return Task.FromResult(default(IEnumerable<T>));
                }

                var results = new List<T>();

                foreach (var responseRow in result.Rows)
                {
                    if (responseRow.Key.ToString() == key)
                    {
                        var resultRow = JsonConvert.DeserializeObject<T>(responseRow.Value);
                        results.Add(resultRow);
                    }
                }

                return Task.FromResult((IEnumerable<T>)results);
            }
        }
    }
}