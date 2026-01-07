using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using YesSql.Data;
using YesSql.Indexes;
using YesSql.Serialization;

namespace YesSql.Commands
{
    public abstract class IndexCommand : IIndexCommand
    {
        protected const string ParameterSuffix = "_$$$";

        protected readonly IStore _store;

        private readonly ConcurrentDictionary<CompoundKey, string> InsertsList = new();
        private readonly ConcurrentDictionary<CompoundKey, string> UpdatesList = new();

        protected static PropertyInfo[] KeysProperties = new[] { typeof(IIndex).GetProperty("Id") };

        public abstract int ExecutionOrder { get; }

        public IndexCommand(IIndex index, IStore store, string collection)
        {
            Index = index;
            _store = store;
            InsertsList = store.TypeService.InsertsList;
            UpdatesList = store.TypeService.UpdatesList;
            Collection = collection;
        }

        public IIndex Index { get; }
        public Document Document { get; }
        public string Collection { get; }

        public abstract Task ExecuteAsync(DbConnection connection, DbTransaction transaction, ISqlDialect dialect, ILogger logger);

   
        protected void GetProperties(DbCommand command, object item, string suffix, ISqlDialect dialect)
        {
            var type = item.GetType();

            foreach (var property in TypePropertiesCache(type))
            {
                var accessor = _store.TypeService.GetPropertyAccessors(property, prop => new PropertyInfoAccessor(prop));

                var value = accessor.Get(item);

                var parameter = command.CreateParameter();
                parameter.ParameterName = property.Name + suffix;
                parameter.Value = dialect.TryConvert(value) ?? DBNull.Value;
                parameter.DbType = dialect.ToDbType(property.PropertyType);
                command.Parameters.Add(parameter);
            }
        }

        protected PropertyInfo[] TypePropertiesCache(Type type)
        {
            return _store.TypeService.GetProperties(type);
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
                            sbColumnList.Append(", ");
                        }
                    }

                    var sbParameterList = new StringBuilder(null);
                    for (var i = 0; i < allProperties.Length; i++)
                    {
                        var property = allProperties.ElementAt(i);

                        sbParameterList.Append('@')
                            .Append(property.Name)
                            .Append(ParameterSuffix);

                        if (i < allProperties.Length - 1)
                        {
                            sbParameterList.Append(", ");
                        }
                    }

                    if (typeof(MapIndex).IsAssignableFrom(type))
                    {
                        // We can set the document id 
                        sbColumnList.Append(", ").Append(dialect.QuoteForColumnName("DocumentId"));
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
                        values.Append(", ");
                    }
                }

                UpdatesList[key] = result = $"update {dialect.QuoteForTableName(_store.Configuration.TablePrefix + _store.Configuration.TableNameConvention.GetIndexTable(type, Collection), _store.Configuration.Schema)} set {values} where {dialect.QuoteForColumnName("Id")} = @Id{ParameterSuffix};";
            }

            return result;
        }



        public abstract bool AddToBatch(ISqlDialect dialect, List<string> queries, DbCommand batchCommand, List<Action<DbDataReader>> actions, int index);

    }
}
