using System;
using System.Collections.Immutable;
using System.Data.Common;

namespace YesSql
{
    public class CommandInterpreterFactory
    {
        private static ImmutableDictionary<Type, Func<ISqlDialect, ICommandInterpreter>> _commandInterpreters = ImmutableDictionary<Type, Func<ISqlDialect, ICommandInterpreter>>.Empty;
        
        /// <summary>
        /// Registers the <see cref="ICommandInterpreter" /> factory for a specific connection type.
        /// </summary>
        public static void Register(Type connectionType, Func<ISqlDialect, ICommandInterpreter> factory)
        {
            // Make the assignment atomic so we can't skip a value
            lock (_commandInterpreters) 
            {
                _commandInterpreters = _commandInterpreters.Remove(connectionType);
                _commandInterpreters = _commandInterpreters.Add(connectionType, factory);
            }
        }

        /// <summary>
        /// Creates an <see cref="ICommandInterpreter" /> instance for a specific connection type.
        /// </summary>
        public static ICommandInterpreter For(DbConnection connection)
        {
            var connectionType = connection.GetType();

            if (!_commandInterpreters.TryGetValue(connectionType, out var factory))
            {
                throw new ArgumentException($"Unknown connection name: {connectionType}");
            }

            var dialect = SqlDialectFactory.For(connection);

            return factory(dialect);
        }
    }
}
