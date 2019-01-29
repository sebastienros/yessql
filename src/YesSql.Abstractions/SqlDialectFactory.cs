using System;
using System.Collections.Concurrent;
using System.Data.Common;

namespace YesSql
{
    public class SqlDialectFactory
    {
        public static readonly ConcurrentDictionary<string, ISqlDialect> SqlDialects = new ConcurrentDictionary<string, ISqlDialect>();

        public static ISqlDialect For(DbConnection connection)
        {
            return For(connection.GetType());
        }

        public static ISqlDialect For(Type dbConnectionType)
        {
            var connectionName = dbConnectionType.Name.ToLower();

            if (!SqlDialects.TryGetValue(connectionName, out var dialect))
            {
                throw new ArgumentException("Unknown connection name: " + connectionName);
            }

            return dialect;
        }
    }
}
