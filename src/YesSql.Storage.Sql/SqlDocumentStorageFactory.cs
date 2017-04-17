using System.Data;
using System.Threading.Tasks;
using YesSql.Services;
using YesSql.Sql;
using YesSql.Storage;

namespace YesSql.Storage.Sql
{
    public class SqlDocumentStorageFactory : IDocumentStorageFactory
    {
        public string TablePrefix { get; set; }

        public IDocumentStorage CreateDocumentStorage(ISession session, IConfiguration configuration)
        {
            return new SqlDocumentStorage(session, this);
        }

        /// <summary>
        /// Creates the necessary tables
        /// </summary>
        public Task InitializeAsync(IConfiguration configuration)
        {
            var connection = configuration.ConnectionFactory.CreateConnection();
            connection.Open();
            try
            {
                using (var transaction = connection.BeginTransaction(configuration.IsolationLevel))
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
                if (configuration.ConnectionFactory.Disposable)
                {
                    connection.Dispose();
                }
                else
                {
                    connection.Close();
                }
            }
#if NET451
            return Task.FromResult(0);
#else
            return Task.CompletedTask;
#endif

        }

        /// <summary>
        /// Creates the necessary tables
        /// </summary>
        public Task InitializeCollectionAsync(IConfiguration configuration, string collection)
        {
            var contentTable = collection + "_" + "Content";
            var connection = configuration.ConnectionFactory.CreateConnection();
            connection.Open();
            try
            {
                using (var transaction = connection.BeginTransaction(configuration.IsolationLevel))
                {
                    var schemaBuilder = new SchemaBuilder(connection, transaction, TablePrefix);

                    schemaBuilder.CreateTable(contentTable, table => table
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
                if (configuration.ConnectionFactory.Disposable)
                {
                    connection.Dispose();
                }
                else
                {
                    connection.Close();
                }
            }
#if NET451
            return Task.FromResult(0);
#else
            return Task.CompletedTask;
#endif
        }
    }
}
