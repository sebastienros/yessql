using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using YesSql.Indexes;

namespace YesSql.Commands
{
    public abstract class IndexCommand : IIndexCommand
    {
        protected readonly string _tablePrefix;

        private static readonly ConcurrentDictionary<string, PropertyInfo[]> TypeProperties = new ConcurrentDictionary<string, PropertyInfo[]>();
        private static readonly ConcurrentDictionary<CompoundKey, string> InsertsList = new ConcurrentDictionary<CompoundKey, string>();
        private static readonly ConcurrentDictionary<CompoundKey, string> UpdatesList = new ConcurrentDictionary<CompoundKey, string>();

        protected static PropertyInfo[] KeysProperties = new[] { typeof(IIndex).GetProperty("Id") };

        public abstract int ExecutionOrder { get; }

        public IndexCommand(IIndex index, string tablePrefix)
        {
            Index = index;
            _tablePrefix = tablePrefix;
        }

        public IIndex Index { get; }
        public Document Document { get; }

        public abstract Task ExecuteAsync(DbConnection connection, DbTransaction transaction, ISqlDialect dialect, ILogger logger);

        public static void ResetQueryCache()
        {
            InsertsList.Clear();
            UpdatesList.Clear();
        }

        protected static PropertyInfo[] TypePropertiesCache(Type type)
        {
            if (TypeProperties.TryGetValue(type.FullName, out PropertyInfo[] pis))
            {
                return pis;
            }

            var properties = type.GetProperties().Where(IsWriteable).ToArray();
            TypeProperties[type.FullName] = properties;
            return properties;
        }

        protected string Inserts(Type type, ISqlDialect dialect)
        {
            var key = new CompoundKey(dialect.Name, type.FullName, _tablePrefix);

            if (!InsertsList.TryGetValue(key, out string result))
            {
                var values = dialect.DefaultValuesInsert;

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
                        sbParameterList.Append("@" + property.Name);
                        if (i < allProperties.Count() - 1)
                        {
                            sbParameterList.Append(", ");
                        }
                    }

                    values = $"({sbColumnList}) VALUES ({sbParameterList})";
                }

                InsertsList[key] = result = $"INSERT INTO {dialect.QuoteForTableName(_tablePrefix + type.Name)} {values} {dialect.IdentitySelectString} {dialect.QuoteForColumnName("Id")}";
            }

            return result;            
        }

        protected string Updates(Type type, ISqlDialect dialect)
        {
            var key = new CompoundKey(dialect.Name, type.FullName, _tablePrefix);

            if (!UpdatesList.TryGetValue(key, out string result))
            {
                var allProperties = TypePropertiesCache(type);
                var values = new StringBuilder(null);

                for (var i = 0; i < allProperties.Length; i++)
                {
                    var property = allProperties[i];
                    values.Append(dialect.QuoteForColumnName(property.Name) + " = @" + property.Name);
                    if (i < allProperties.Length - 1)
                    {
                        values.Append(", ");
                    }
                }

                UpdatesList[key] = result = $"UPDATE {dialect.QuoteForTableName(_tablePrefix + type.Name)} SET {values} WHERE {dialect.QuoteForColumnName("Id")} = @Id;";
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

        public struct CompoundKey : IEquatable<CompoundKey>
        {
            private string _key1;
            private string _key2;
            private string _key3;

            public CompoundKey(string key1, string key2, string key3)
            {
                _key1 = key1;
                _key2 = key2;
                _key3 = key3;
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
                    
                    return hashCode;
                }
            }
        }
    }
}
