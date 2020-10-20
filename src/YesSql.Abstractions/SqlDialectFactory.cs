using System;
using System.Collections.Immutable;
using System.Data.Common;

namespace YesSql
{
    public class SqlDialectFactory
    {
        private static ImmutableDictionary<Type, ISqlDialect> _sqlDialects = ImmutableDictionary<Type, ISqlDialect>.Empty;

        /// <summary>
        /// Registers the <see cref="ISqlDialect" /> for a specific connection type.
        /// </summary>
        public static void Register(Type connectionType, ISqlDialect sqlDialect)
        {
            // Make the assignment atomic so we can't skip a value
            lock (_sqlDialects) 
            {
                _sqlDialects = _sqlDialects.Remove(connectionType);
                _sqlDialects = _sqlDialects.Add(connectionType, sqlDialect);
            }
        }

        /// <summary>
        /// Returns the <see cref="ISqlDialect" /> instance for a specific connection.
        /// </summary>
        public static ISqlDialect For(DbConnection connection)
        {
            return For(connection.GetType());
        }

        /// <summary>
        /// Returns the <see cref="ISqlDialect" /> instance for a specific connection type.
        /// </summary>
        public static ISqlDialect For(Type dbConnectionType)
        {
            if (!_sqlDialects.TryGetValue(dbConnectionType, out var dialect))
            {
                throw new ArgumentException($"Unknown connection type: {dbConnectionType}");
            }

            return dialect;
        }
    }
}
