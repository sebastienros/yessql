using System;
using System.Collections.Concurrent;
using System.Data;

namespace YesSql
{
    public class CommandInterpreterFactory
    {
        public static readonly ConcurrentDictionary<string, Func<ISqlDialect, ICommandInterpreter>> CommandInterpreters = new ConcurrentDictionary<string, Func<ISqlDialect, ICommandInterpreter>>();
        
        public static ICommandInterpreter For(IDbConnection connection)
        {
            var connectionString = connection.ConnectionString;

            if (!CommandInterpreters.ContainsKey(connectionString))
            {
                throw new ArgumentException($"The connection string '{connectionString}' doesn't contains a dialect parameter.");
            }

            var dialect = SqlDialectFactory.For(connection);

            return CommandInterpreters[connectionString](dialect);
        }
    }

}
