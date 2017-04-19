using System;
using System.Collections.Concurrent;
using System.Data;

namespace YesSql
{
    public class SqlDialectFactory
    {
        public static readonly ConcurrentDictionary<string, ISqlDialect> SqlDialects = new ConcurrentDictionary<string, ISqlDialect>();

        public static ISqlDialect For(IDbConnection connection)
        {
            string connectionName = connection.GetType().Name.ToLower();

            if (!SqlDialects.TryGetValue(connectionName, out ISqlDialect dialect))
            {
                throw new ArgumentException("Unknown connection name: " + connectionName);
            }

            return dialect;
        }
    }
}
