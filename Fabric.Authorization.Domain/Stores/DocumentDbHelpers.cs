using System;
using System.Collections.Generic;
using System.Text;

namespace Fabric.Authorization.Domain.Stores
{
    public static class DocumentDbHelpers
    {
        public static string GetFullDocumentId<T>(string documentId)
        {
            return $"{typeof(T).Name.ToLowerInvariant()}:{documentId}";
        }
    }
}
