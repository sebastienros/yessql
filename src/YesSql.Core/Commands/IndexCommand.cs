using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using YesSql.Indexes;
using YesSql.Serialization;

namespace YesSql.Commands
{
    public abstract class IndexCommand : IIndexCommand
    {
        protected const string ParameterSuffix = "_$$$";
        private const string _separator = ", ";

        protected readonly IStore _store;

        private static readonly ConcurrentDictionary<PropertyInfo, PropertyInfoAccessor> PropertyAccessors = new();
        private static readonly ConcurrentDictionary<string, PropertyInfo[]> TypeProperties = new();
        private static readonly ConcurrentDictionary<CompoundKey, string> InsertsList = new();
        private static readonly ConcurrentDictionary<CompoundKey, string> UpdatesList = new();

        protected static PropertyInfo[] KeysProperties = [typeof(IIndex).GetProperty("Id")];

        public abstract int ExecutionOrder { get; }

        public IndexCommand(IIndex index, IStore store, string collection)
        {
            Index = index;
            _store = store;
            Collection = collection;
        }

        public IIndex Index { get; }
        public Document Document { get; }
        public string Collection { get; }

        public abstract Task ExecuteAsync(DbConnection connection, DbTransaction transaction, ISqlDialect dialect, ILogger logger);

        public static void ResetQueryCache()
        {
            InsertsList.Clear();
            UpdatesList.Clear();
        }

        protected static void GetProperties(DbCommand command, object item, string suffix, ISqlDialect dialect)
        {
            var type = item.GetType();

            foreach (var property in TypePropertiesCache(type))
            {
                var accessor = PropertyAccessors.GetOrAdd(property, p => new PropertyInfoAccessor(p));

                var value = accessor.Get(item);

                var parameter = command.CreateParameter();
                parameter.ParameterName = property.Name + suffix;
                parameter.Value = dialect.TryConvert(value) ?? DBNull.Value;
                parameter.DbType = dialect.ToDbType(property.PropertyType);
                command.Parameters.Add(parameter);
            }
        }

        protected static PropertyInfo[] TypePropertiesCache(Type type)
        {
            if (TypeProperties.TryGetValue(type.FullName, out var pis))
            {
                return pis;
            }

            var properties = type.GetProperties().Where(IsWriteable).ToArray();
            TypeProperties[type.FullName] = properties;
            return properties;
        }

        protected string Inserts(Type type, ISqlDialect dialect)
        {
            var key = new CompoundKey(
                dialect.Name,
                type.FullName,
                _store.Configuration.Schema,
                _store.Configuration.TablePrefix,
                Collection);

            if (!InsertsList.TryGetValue(key, out var result))
            {
                string values;

                var allProperties = TypePropertiesCache(type);

                if (allProperties.Length > 0)
                {
                    var sbColumnList = new StringBuilder(null);

                    for (var i = 0; i < allProperties.Length; i++)
                    {
                        var property = allProperties.ElementAt(i);
                        sbColumnList.Append(dialect.QuoteForColumnName(property.Name));
                        if (i < allProperties.Length - 1)
                        {
                            sbColumnList.Append(_separator);
                        }
                    }

                    var sbParameterList = new StringBuilder(null);
                    for (var i = 0; i < allProperties.Length; i++)
                    {
                        var property = allProperties.ElementAt(i);
                        sbParameterList.Append("@").Append(property.Name).Append(ParameterSuffix);
                        if (i < allProperties.Length - 1)
                        {
                            sbParameterList.Append(_separator);
                        }
                    }

                    if (typeof(MapIndex).IsAssignableFrom(type))
                    {
                        // We can set the document id 
                        sbColumnList.Append(_separator).Append(dialect.QuoteForColumnName("DocumentId"));
                        sbParameterList.Append(", @DocumentId").Append(ParameterSuffix);
                    }

                    values = $"({sbColumnList}) values ({sbParameterList})";
                }
                else
                {
                    if (typeof(MapIndex).IsAssignableFrom(type))
                    {
                        values = $"({dialect.QuoteForColumnName("DocumentId")}) values (@DocumentId{ParameterSuffix})";
                    }
                    else
                    {
                        values = dialect.DefaultValuesInsert;
                    }
                }

                InsertsList[key] = result = $"insert into {dialect.QuoteForTableName(_store.Configuration.TablePrefix + _store.Configuration.TableNameConvention.GetIndexTable(type, Collection), _store.Configuration.Schema)} {values} {dialect.IdentitySelectString} {dialect.QuoteForColumnName("Id")};";
            }

            return result;
        }

        protected string Updates(Type type, ISqlDialect dialect)
        {
            var key = new CompoundKey(
                dialect.Name,
                type.FullName,
                _store.Configuration.Schema,
                _store.Configuration.TablePrefix,
                Collection);

            if (!UpdatesList.TryGetValue(key, out var result))
            {
                var allProperties = TypePropertiesCache(type);
                var values = new StringBuilder(null);

                for (var i = 0; i < allProperties.Length; i++)
                {
                    var property = allProperties[i];
                    values.Append(dialect.QuoteForColumnName(property.Name) + " = @" + property.Name + ParameterSuffix);
                    if (i < allProperties.Length - 1)
                    {
                        values.Append(_separator);
                    }
                }

                UpdatesList[key] = result = $"update {dialect.QuoteForTableName(_store.Configuration.TablePrefix + _store.Configuration.TableNameConvention.GetIndexTable(type, Collection), _store.Configuration.Schema)} set {values} where {dialect.QuoteForColumnName("Id")} = @Id{ParameterSuffix};";
            }

            return result;
        }

        private static bool IsWriteable(PropertyInfo pi)
        {
            return
                pi.Name != nameof(IIndex.Id) &&
                // don't read DocumentId when on a MapIndex as it might be used to 
                // read the DocumentId directly from an Index query
                pi.Name != "DocumentId"
                ;
        }

        public abstract bool AddToBatch(ISqlDialect dialect, List<string> queries, DbCommand batchCommand, List<Action<DbDataReader>> actions, int index);

        private record CompoundKey(string Dialect, string Type, string Schema, string Prefix, string Collection);
    }
}
