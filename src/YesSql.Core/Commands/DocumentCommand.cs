using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace YesSql.Commands
{
    public abstract class DocumentCommand : IIndexCommand, ICollectionName
    {
        protected static readonly PropertyInfo[] AllProperties = 
        {
            typeof(Document).GetProperty("Type")
        };

        protected static readonly PropertyInfo[] AllKeys = 
        {
            typeof(Document).GetProperty("Id")
        };

        public abstract int ExecutionOrder { get; }

        public DocumentCommand(Document document, string collection)
        {

            Document = document;
            Collection = collection;
        }

        public Document Document { get; }

        public string Collection { get; }

        public abstract Task ExecuteAsync(DbConnection connection, DbTransaction transaction, ISqlDialect dialect, ILogger logger);

        public abstract bool AddToBatch(ISqlDialect dialect, List<string> queries, DbCommand parameters, List<Action<DbDataReader>> actions, int index);
    }
}
