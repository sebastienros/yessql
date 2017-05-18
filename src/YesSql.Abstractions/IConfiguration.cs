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
    }
    
    public class DbConnectionFactory<TDbConnection> : IConnectionFactory
        where TDbConnection : DbConnection, new()
    {
        private readonly bool _reuseConnection;
        private TDbConnection _reusedConnection;
        private readonly string _connectionString;

        public DbConnectionFactory(string connectionString, bool reuseConnection = false)
        {
            _reuseConnection = reuseConnection;
            _connectionString = connectionString;
        }

        public bool Disposable => !_reuseConnection;

        public IDbConnection CreateConnection()
        {
            if (_reuseConnection)
            {
                if (_reusedConnection == null)
                {
                    _reusedConnection = new TDbConnection();
                    _reusedConnection.ConnectionString = _connectionString;
                }

                return _reusedConnection;
            }

            var connection = new TDbConnection();
            connection.ConnectionString = _connectionString;

            return connection;
        }

        public void Dispose()
        {
            if (_reuseConnection)
            {
                if (_reusedConnection != null)
                {
                    _reusedConnection.Dispose();
                }
            }
        }
    }
}
