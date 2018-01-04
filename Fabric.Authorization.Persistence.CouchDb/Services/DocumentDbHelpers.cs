namespace Fabric.Authorization.Persistence.CouchDb.Services
{
    public static class DocumentDbHelpers
    {
        public static string GetFullDocumentId<T>(string documentId)
        {
            return $"{typeof(T).Name.ToLowerInvariant()}:{documentId}";
        }
    }
}