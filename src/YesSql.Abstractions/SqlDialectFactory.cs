using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YesSql
{
    public class SqlDialectFactory
    {
        public static Dictionary<string, ISqlDialect> SqlDialects { get; } = new Dictionary<string, ISqlDialect>();

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
