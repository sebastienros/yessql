using Dapper;
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

        protected readonly IStore _store;

        private static readonly ConcurrentDictionary<PropertyInfo, PropertyInfoAccessor> PropertyAccessors = new ConcurrentDictionary<PropertyInfo, PropertyInfoAccessor>();
        private static readonly ConcurrentDictionary<string, PropertyInfo[]> TypeProperties = new ConcurrentDictionary<string, PropertyInfo[]>();
        private static readonly ConcurrentDictionary<CompoundKey, string> InsertsList = new ConcurrentDictionary<CompoundKey, string>();
        private static readonly ConcurrentDictionary<CompoundKey, string> UpdatesList = new ConcurrentDictionary<CompoundKey, string>();

        protected static PropertyInfo[] KeysProperties = new[] { typeof(IIndex).GetProperty("Id") };

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

        protected static void GetProperties(DynamicParameters parameters, object item, string suffix, ISqlDialect dialect)
        {
            var type = item.GetType();

            foreach (var property in TypePropertiesCache(type))
            {
                var accessor = PropertyAccessors.GetOrAdd(property, p => new PropertyInfoAccessor(p));

                var value = accessor.Get(item);

                if (dialect.TryConvert(value, property.PropertyType, out var converted))
                {
                    value = converted;
                }

                parameters.Add(property.Name + suffix, value, dialect.ToDbType(property.PropertyType));
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
            var key = new CompoundKey(dialect.Name, type.FullName, _store.Configuration.TablePrefix, Collection);

            if (!InsertsList.TryGetValue(key, out var result))
            {
                string values;

                var allProperties = TypePropertiesCache(type);

                if (allProperties.Any())
                {
                    var sbColumnList = new StringBuilder(null);

                    for (var i = 0; i < allProperties.Count(); i++)
                    {
                        var property = allProperties.ElementAt(i);
                        sbColumnList.Append(dialect.QuoteForColumnName(property.Name));
                        if (i < allProperties.Count() - 1)
                        {
                            sbColumnList.Append(", ");
                        }
                    }

                    var sbParameterList = new StringBuilder(null);
                    for (var i = 0; i < allProperties.Count(); i++)
                    {
                        var property = allProperties.ElementAt(i);
                        sbParameterList.Append("@").Append(property.Name).Append(ParameterSuffix);
                        if (i < allProperties.Count() - 1)
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

                InsertsList[key] = result = $"insert into {dialect.QuoteForTableName(_store.Configuration.TablePrefix + _store.Configuration.TableNameConvention.GetIndexTable(type, Collection))} {values} {dialect.IdentitySelectString} {dialect.QuoteForColumnName("Id")};";
            }

            return result;            
        }

        protected string Updates(Type type, ISqlDialect dialect)
        {
            var key = new CompoundKey(dialect.Name, type.FullName, _store.Configuration.TablePrefix, Collection);

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

                UpdatesList[key] = result = $"update {dialect.QuoteForTableName(_store.Configuration.TablePrefix + _store.Configuration.TableNameConvention.GetIndexTable(type, Collection))} set {values} where {dialect.QuoteForColumnName("Id")} = @Id{ParameterSuffix};";
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

        public abstract bool AddToBatch(ISqlDialect dialect, List<string> queries, DynamicParameters parameters, List<Action<DbDataReader>> actions);

        public struct CompoundKey : IEquatable<CompoundKey>
        {
            private readonly string _key1;
            private readonly string _key2;
            private readonly string _key3;
            private readonly string _key4;

            public CompoundKey(string key1, string key2, string key3, string key4)
            {
                _key1 = key1;
                _key2 = key2;
                _key3 = key3;
                _key4 = key4;
            }

            /// <inheritdoc />
            public override bool Equals(object obj)
            {
                if (obj is CompoundKey other)
                {
                    return Equals(other);
                }

                return false;
            }

            /// <inheritdoc />
            public bool Equals(CompoundKey other)
            {
                return String.Equals(_key1, other._key1)
                    && String.Equals(_key2, other._key2)
                    && String.Equals(_key3, other._key3)
                    && String.Equals(_key4, other._key4)
                    ;
            }

            /// <inheritdoc />
            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = 13;
                    hashCode = (hashCode * 397) ^ (!string.IsNullOrEmpty(_key1) ? _key1.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (!string.IsNullOrEmpty(_key2) ? _key2.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (!string.IsNullOrEmpty(_key3) ? _key3.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (!string.IsNullOrEmpty(_key4) ? _key4.GetHashCode() : 0);

                    return hashCode;
                }
            }
        }
    }
}
