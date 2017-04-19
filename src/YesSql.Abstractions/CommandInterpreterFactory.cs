using System;
using System.Collections.Generic;
using System.Data;

namespace YesSql
{
    public class CommandInterpreterFactory
    {
        public static readonly Dictionary<string, Func<ISqlDialect, ICommandInterpreter>> CommandInterpreters = new Dictionary<string, Func<ISqlDialect, ICommandInterpreter>>();
        
        public static ICommandInterpreter For(IDbConnection connection)
        {
            string connectionName = connection.GetType().Name.ToLower();

            if (!CommandInterpreters.ContainsKey(connectionName))
            {
                throw new ArgumentException("Unknown connection name: " + connectionName);
            }

            var dialect = SqlDialectFactory.For(connection);

            return CommandInterpreters[connectionName](dialect);
        }
    }

}
