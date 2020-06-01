using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;
using System.Threading.Tasks;

namespace YesSql.Commands
{
    public abstract class DocumentCommand : IIndexCommand
    {
        protected static readonly PropertyInfo[] AllProperties = new PropertyInfo[]
        {
            typeof(Document).GetProperty("Type")
        };

        protected static readonly PropertyInfo[] AllKeys = new PropertyInfo[]
        {
            typeof(Document).GetProperty("Id")
        };

        public abstract int ExecutionOrder { get; }

        public DocumentCommand(string collectionName, Document document)
        {
            CollectionName = collectionName;
            Documents = new[] { document };
        }

        public DocumentCommand(string collectionSafeName, IEnumerable<Document> documents)
        {
            CollectionName = collectionSafeName;
            Documents = documents;
        }

        public string CollectionName { get; }

        public IEnumerable<Document> Documents { get; }

        public abstract Task ExecuteAsync(DbConnection connection, DbTransaction transaction, ISqlDialect dialect, ILogger logger);
    }
}
