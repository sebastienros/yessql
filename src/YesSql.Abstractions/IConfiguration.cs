using System;
using System.Data;
using System.Data.Common;

namespace YesSql
{
    public interface IConfiguration
    {
        IIdentifierFactory IdentifierFactory { get; set; }
        IsolationLevel IsolationLevel { get; set; }
        IConnectionFactory ConnectionFactory { get; set; }
        IContentSerializer ContentSerializer { get; set; }
        string TablePrefix { get; set; }
        int SessionPoolSize { get; set; }
        bool QueryGatingEnabled { get; set; }
    }

    public static class ConfigurationExtensions
    {
        public static IConfiguration SetIdentifierFactory(this IConfiguration configuration, IIdentifierFactory identifierFactory)
        {
            configuration.IdentifierFactory = identifierFactory;
            return configuration;
        }

        public static IConfiguration SetIsolationLevel(this IConfiguration configuration, IsolationLevel isolationLevel)
        {
            configuration.IsolationLevel = isolationLevel;
            return configuration;
        }

        public static IConfiguration SetConnectionFactory(this IConfiguration configuration, IConnectionFactory connectionFactory)
        {
            configuration.ConnectionFactory = connectionFactory;
            return configuration;
        }

        public static IConfiguration SetTablePrefix(this IConfiguration configuration, string tablePrefix)
        {
            configuration.TablePrefix = tablePrefix;
            return configuration;
        }

        public static IConfiguration SetContentSerializer(this IConfiguration configuration, IContentSerializer contentSerializer)
        {
            configuration.ContentSerializer = contentSerializer;
            return configuration;
        }

        public static IConfiguration SetSessionPoolSize(this IConfiguration configuration, int size)
        {
            configuration.SessionPoolSize = size;
            return configuration;
        }

        public static IConfiguration DisableQueryGating(this IConfiguration configuration)
        {
            configuration.QueryGatingEnabled = false;
            return configuration;
        }
    }

    public class DbConnectionFactory<TDbConnection> : IConnectionFactory
        where TDbConnection : DbConnection, new()
    {
        private readonly bool _shareConnection;
        private TDbConnection _sharedConnection;
        private readonly string _connectionString;
        private bool _disposing;

        public Type DbConnectionType => typeof(TDbConnection);

        public DbConnectionFactory(string connectionString, bool shareConnection = false)
        {
            _shareConnection = shareConnection;
            _connectionString = connectionString;
        }

        public IDbConnection CreateConnection()
        {
            if (_shareConnection)
            {
                if (_sharedConnection == null)
                {
                    lock (this)
                    {
                        if (_sharedConnection == null)
                        {
                            _sharedConnection = new TDbConnection();
                            _sharedConnection.ConnectionString = _connectionString;
                        }
                    }
                }

                return _sharedConnection;
            }

            var connection = new TDbConnection();
            connection.ConnectionString = _connectionString;

            return connection;
        }

        public void CloseConnection(IDbConnection connection)
        {
            if (_shareConnection)
            {
                // If the connection is shared, we don't close it
                return;
            }

            if (connection != null)
            {
                connection.Close();
            }
        }

        public void Dispose()
        {
            if (_disposing)
            {
                return;
            }

            _disposing = true;

            if (_shareConnection)
            {
                if (_sharedConnection != null)
                {
                    _sharedConnection.Dispose();
                }
            }
        }
    }
}
