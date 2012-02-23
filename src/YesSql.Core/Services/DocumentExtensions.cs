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
        public static IEnumerable<T> As<T>(this IEnumerable<Document> documents)
        {
            IDocumentSerializer serializer = new JSonSerializer();
            return documents.Select(serializer.Deserialize).Cast<T>();
        }

        public static T As<T>(this Document document) where T : class
        {
            if(document == null)
            {
                return null;
            }

            IDocumentSerializer serializer = new JSonSerializer();
            return serializer.Deserialize(document) as T;
        }
    }
}
