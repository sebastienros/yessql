using System.Collections.Generic;
using System.Linq;
using YesSql.Core.Data.Models;
using YesSql.Core.Serialization;

namespace YesSql.Core.Services {
    /// <summary>
    /// Used to convert <see cref="Document"/> instances to dynamic objects
    /// </summary>
    public static class DocumentExtensions
    {
        public static IEnumerable<T> ConvertTo<T>(this IStore store, IEnumerable<Document> documents) where T : class
        {
            IDocumentSerializer serializer = store.GetDocumentSerializer();
            return documents.Select(d => serializer.Deserialize(d) as T);
        }

        public static T ConvertTo<T>(this IStore store, Document document) where T : class
        {
            if(document == null)
            {
                return null;
            }

            IDocumentSerializer serializer = store.GetDocumentSerializer();
            return serializer.Deserialize(document) as T;
        }
    }
}
