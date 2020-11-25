using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace YesSql
{
    /// <summary>
    /// This class provides methods to create <see cref="DbConnection" /> instances of a concrete type.
    /// </summary>
    /// <typeparam name="TDbConnection">The concrete <see cref="DbConnection" /> implementation to instantiate.</typeparam>
    public class DbConnectionFactory<TDbConnection> : IConnectionFactory
            where TDbConnection : DbConnection, new()
    {
        private readonly string _connectionString;

        public Type DbConnectionType => typeof(TDbConnection);

        public DbConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public DbConnection CreateConnection()
        {
            var connection = new TDbConnection
            {
                ConnectionString = _connectionString
            };

            return connection;
        }
    }
}
