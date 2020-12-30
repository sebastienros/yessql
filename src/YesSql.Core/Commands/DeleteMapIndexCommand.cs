using Dapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace YesSql.Commands
{
    public sealed class DeleteMapIndexCommand : IIndexCommand, ICollectionName
    {
        private readonly IStore _store;
        public int DocumentId { get; }
        public Type IndexType { get; }
        public string Collection { get; }
        public int ExecutionOrder { get; } = 1;

        public DeleteMapIndexCommand(Type indexType, int documentId, IStore store, string collection)
        {
            IndexType = indexType;
            DocumentId = documentId;
            Collection = collection;
            _store = store;
        }

        public Task ExecuteAsync(DbConnection connection, DbTransaction transaction, ISqlDialect dialect, ILogger logger )
        {
            var command = $"delete from {dialect.QuoteForTableName(_store.Configuration.TablePrefix + _store.Configuration.TableNameConvention.GetIndexTable(IndexType, Collection))} where {dialect.QuoteForColumnName("DocumentId")} = @Id;";
            logger.LogTrace(command);
            return connection.ExecuteAsync(command, new { Id = DocumentId }, transaction);
        }

        public bool AddToBatch(ISqlDialect dialect, List<string> queries, Dictionary<string, object> parameters, List<Action<DbDataReader>> actions)
        {
            var index = queries.Count;

            var command = $"delete from {dialect.QuoteForTableName(_store.Configuration.TablePrefix + _store.Configuration.TableNameConvention.GetIndexTable(IndexType, Collection))} where {dialect.QuoteForColumnName("DocumentId")} = @Id_{index};";

            queries.Add(command);

            parameters["Id_" + index] = DocumentId;

            return true;
        }
    }
}
