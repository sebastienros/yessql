using System;
using System.Data;
using System.Threading.Tasks;
using YesSql.Core.Services;
using YesSql.Core.Sql;
using YesSql.Core.Storage;

namespace YesSql.Storage.Sql
{
    public class SqlDocumentStorageFactory : IDocumentStorageFactory
    {
        public IsolationLevel IsolationLevel { get; set; } = IsolationLevel.ReadCommitted;
        public IConnectionFactory ConnectionFactory { get; set; }
        public string TablePrefix { get; set; }

        public SqlDocumentStorageFactory(IConnectionFactory connectionFactory)
        {
            ConnectionFactory = connectionFactory;
        }

        public IDocumentStorage CreateDocumentStorage()
        {
            return new SqlDocumentStorage(this);
        }

        /// <summary>
        /// Creates the necessary tables
        /// </summary>
        public async Task InitializeAsync()
        {
            var connection = ConnectionFactory.CreateConnection();
            await connection.OpenAsync();
            try
            {
                using (var transaction = connection.BeginTransaction(IsolationLevel))
                {
                    var schemaBuilder = new SchemaBuilder(connection, transaction, TablePrefix);

                    schemaBuilder.CreateTable("Content", table => table
                        .Column<int>("Id", column => column
                            .PrimaryKey().NotNull())
                        .Column<string>("Content", column => column
                            .Unlimited()
                    ));

                    transaction.Commit();
                }
            }
            finally
            {
                if (ConnectionFactory.Disposable)
                {
                    connection.Dispose();
                }
                else
                {
                    connection.Close();
                }
            }
        }
    }
}
