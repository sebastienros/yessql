using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using YesSql.Core.Data;
using YesSql.Core.Sql;
using YesSql.Core.Storage;

namespace YesSql.Core.Services
{
    public class Configuration
    {
        internal Configuration()
        {
            IdentifierFactory = new DefaultIdentifierFactory();
            IsolationLevel = IsolationLevel.ReadCommitted;
            Migrations = new List<Action<SchemaBuilder>>();
        }

        public IIdentifierFactory IdentifierFactory { get; set; }
        public IDocumentStorageFactory DocumentStorageFactory { get; set; }
        public IsolationLevel IsolationLevel { get; set; }
        public IConnectionFactory ConnectionFactory { get; set; }
        public List<Action<SchemaBuilder>> Migrations { get; }
        public void RunDefaultMigration()
        {
            // Document
            // This table should be part of the default migration code, and 
            // its version also created in the migration table

            Migrations.Insert(0, builder => builder
                .CreateTable("Document", table => table
                    .Column<int>("Id", column => column.PrimaryKey().Identity().NotNull())
                    .Column<string>("Type", column => column.NotNull())
                )
                .AlterTable("Document", table => table
                    .CreateIndex("IX_Type", "Type")
                )
            );
        }

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
        where TDbConnection : DbConnection
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
                    _connection = (TDbConnection) Activator.CreateInstance(typeof(TDbConnection), _connectionString);
                }

                return _connection;
            }

            return (TDbConnection)Activator.CreateInstance(typeof(TDbConnection), _connectionString);
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
