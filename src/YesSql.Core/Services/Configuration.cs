using System;
using System.Data;
using System.Data.Common;
using YesSql.Core.Data;
using YesSql.Core.Storage;

namespace YesSql.Core.Services
{
    public class Configuration
    {
        internal Configuration()
        {
            IdentifierFactory = new DefaultIdentifierFactory();
            IsolationLevel = IsolationLevel.ReadCommitted;
            TablePrefix = "";
        }

        public IIdentifierFactory IdentifierFactory { get; set; }
        public IDocumentStorageFactory DocumentStorageFactory { get; set; }
        public IsolationLevel IsolationLevel { get; set; }
        public IConnectionFactory ConnectionFactory { get; set; }
        public string TablePrefix { get; set; }
    }

    public interface IConnectionFactory : IDisposable
    {
        DbConnection CreateConnection();

        /// <summary>
        /// <c>true</c> if the created connection can be disposed by the client.
        /// </summary>
        bool Disposable { get; }
    }

    public class DbConnectionFactory<TDbConnection> : IConnectionFactory
        where TDbConnection : DbConnection, new()
    {
        private readonly bool _reuseConnection;
        private TDbConnection _connection;
        private readonly string _connectionString;

        public DbConnectionFactory(string connectionString, bool reuseConnection = false)
        {
            _reuseConnection = reuseConnection;
            _connectionString = connectionString;
        }

        public bool Disposable => !_reuseConnection;

        public DbConnection CreateConnection()
        {
            if(_reuseConnection)
            {
                if (_connection == null)
                {
                    _connection = new TDbConnection();
                    _connection.ConnectionString = _connectionString;
                }

                return _connection;
            }

            _connection = new TDbConnection();
            _connection.ConnectionString = _connectionString;

            return _connection;
        }

        public void Dispose()
        {
            if(_reuseConnection)
            {
                if (_connection != null)
                {
                    _connection.Dispose();
                }
            }
        }
    }
}
