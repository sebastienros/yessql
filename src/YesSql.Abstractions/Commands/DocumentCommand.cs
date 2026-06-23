using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using YesSql.Commands.DocumentChanged;

namespace YesSql.Commands
{
    public abstract class DocumentCommand : IIndexCommand, ICollectionName
    {
        protected static readonly PropertyInfo[] AllProperties =
        [
            typeof(Document).GetProperty("Type")
        ];

        protected static readonly PropertyInfo[] AllKeys =
        [
            typeof(Document).GetProperty("Id")
        ];

        public abstract int ExecutionOrder { get; }

        protected DocumentCommand(Document document, string collection)
        {
            Document = document;
            Collection = collection;
        }

        public Document Document { get; }

        public string Collection { get; }

        protected DocumentChangeContext CreateContext(ISession session, object entity, IStore store, DbConnection connection, DbTransaction transaction, ISqlDialect dialect)
            => new()
            {
                Session = session,
                Entity = entity,
                Document = Document,
                Store = store,
                Connection = connection,
                Transaction = transaction,
                Dialect = dialect
            };

        protected DocumentChangeInBatchContext CreateBatchContext(ISession session, object entity, DbCommand batchCommand, List<string> queries)
            => new()
            {
                Session = session,
                Entity = entity,
                Document = Document,
                BatchCommand = batchCommand,
                Queries = queries
            };

        public abstract Task ExecuteAsync(DbConnection connection, DbTransaction transaction, ISqlDialect dialect, ILogger logger, CancellationToken cancellationToken = default);

        public abstract bool AddToBatch(ISqlDialect dialect, List<string> queries, DbCommand parameters, List<Action<DbDataReader>> actions, int index);
    }
}
