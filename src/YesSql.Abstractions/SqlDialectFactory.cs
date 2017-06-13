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
            var connectionString = connection.ConnectionString;

            if (!SqlDialects.TryGetValue(connectionString, out ISqlDialect dialect))
            {
                throw new ArgumentException($"The connection string '{connectionString}' doesn't contains a dialect parameter.");
            }

            return dialect;
        }
    }
}
