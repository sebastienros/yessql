using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;
using System.Threading;
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

        private static readonly ConcurrentDictionary<DocumentCommandKey, string> InsertCommands = new();
        private static readonly ConcurrentDictionary<DocumentCommandKey, string> UpdateCommands = new();
        private static readonly ConcurrentDictionary<DocumentCommandKey, string> DeleteCommands = new();

        public abstract int ExecutionOrder { get; }

        protected DocumentCommand(Document document, string collection)
        {
            Document = document;
            Collection = collection;
        }

        public Document Document { get; }

        public string Collection { get; }

        public abstract Task ExecuteAsync(DbConnection connection, DbTransaction transaction, ISqlDialect dialect, ILogger logger, CancellationToken cancellationToken = default);

        public abstract bool AddToBatch(ISqlDialect dialect, List<string> queries, DbCommand parameters, List<Action<DbDataReader>> actions, int index);

        /// <summary>
        /// Clears the cached document SQL statements. Invoked when a store is (re)initialized.
        /// </summary>
        public static void ResetQueryCache()
        {
            InsertCommands.Clear();
            UpdateCommands.Clear();
            DeleteCommands.Clear();
        }

        /// <summary>
        /// Returns the cached <c>insert</c> statement for a document table. The statement only depends on
        /// the dialect, schema, table prefix and collection, so it can be reused across commands.
        /// </summary>
        protected static string GetInsertCommandText(ISqlDialect dialect, IStore store, string collection)
        {
            var key = new DocumentCommandKey(dialect.Name, store.Configuration.Schema, store.Configuration.TablePrefix, collection);

            if (!InsertCommands.TryGetValue(key, out var result))
            {
                var documentTable = store.Configuration.TableNameConvention.GetDocumentTable(collection);

                InsertCommands[key] = result = $"insert into {dialect.QuoteForTableName(store.Configuration.TablePrefix + documentTable, store.Configuration.Schema)} ({dialect.QuoteForColumnName("Id")}, {dialect.QuoteForColumnName("Type")}, {dialect.QuoteForColumnName("Content")}, {dialect.QuoteForColumnName("Version")}) values (@Id, @Type, @Content, @Version);";
            }

            return result;
        }

        /// <summary>
        /// Returns the cached <c>update</c> statement (without the optional version check clause) for a document table.
        /// </summary>
        protected static string GetUpdateCommandText(ISqlDialect dialect, IStore store, string collection)
        {
            var key = new DocumentCommandKey(dialect.Name, store.Configuration.Schema, store.Configuration.TablePrefix, collection);

            if (!UpdateCommands.TryGetValue(key, out var result))
            {
                var documentTable = store.Configuration.TableNameConvention.GetDocumentTable(collection);

                UpdateCommands[key] = result = $"update {dialect.QuoteForTableName(store.Configuration.TablePrefix + documentTable, store.Configuration.Schema)} "
                    + $"set {dialect.QuoteForColumnName("Content")} = @Content, {dialect.QuoteForColumnName("Version")} = @Version where "
                    + $"{dialect.QuoteForColumnName("Id")} = @Id ";
            }

            return result;
        }

        /// <summary>
        /// Returns the cached <c>delete</c> statement for a document table.
        /// </summary>
        protected static string GetDeleteCommandText(ISqlDialect dialect, IStore store, string collection)
        {
            var key = new DocumentCommandKey(dialect.Name, store.Configuration.Schema, store.Configuration.TablePrefix, collection);

            if (!DeleteCommands.TryGetValue(key, out var result))
            {
                var documentTable = store.Configuration.TableNameConvention.GetDocumentTable(collection);

                DeleteCommands[key] = result = $"delete from {dialect.QuoteForTableName(store.Configuration.TablePrefix + documentTable, store.Configuration.Schema)} where {dialect.QuoteForColumnName("Id")} = @Id;";
            }

            return result;
        }

        private sealed record DocumentCommandKey(string Dialect, string Schema, string Prefix, string Collection);
    }
}
