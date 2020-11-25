using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;
using System.Threading.Tasks;

namespace YesSql.Commands
{
    public abstract class DocumentCommand : IIndexCommand, ICollectionName
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

        public DocumentCommand(Document document, string collection) :
            this(new[] { document }, collection)
        {
        }

        public DocumentCommand(IEnumerable<Document> documents, string collection)
        {
            Documents = documents;
            Collection = collection;
        }

        public IEnumerable<Document> Documents { get; }

        public string Collection { get; }

        public abstract Task ExecuteAsync(DbConnection connection, DbTransaction transaction, ISqlDialect dialect, ILogger logger);
    }
}
