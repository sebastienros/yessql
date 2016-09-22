using System.Threading.Tasks;
using YesSql.Core.Services;
using YesSql.Core.Sql;
using YesSql.Core.Storage;

namespace YesSql.Storage.Sql
{
    public class SqlDocumentStorageFactory : IDocumentStorageFactory
    {
        public string TablePrefix { get; set; }

        public IDocumentStorage CreateDocumentStorage(ISession session, Configuration configuration)
        {
            return new SqlDocumentStorage(session, this);
        }

        /// <summary>
        /// Creates the necessary tables
        /// </summary>
        public async Task InitializeAsync(Configuration configuration)
        {
            var connection = configuration.ConnectionFactory.CreateConnection();
            await connection.OpenAsync();
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
        }

        /// <summary>
        /// Creates the necessary tables
        /// </summary>
        public async Task InitializeCollectionAsync(Configuration configuration, string collection)
        {
            var contentTable = collection + "_" + "Content";
            var connection = configuration.ConnectionFactory.CreateConnection();
            await connection.OpenAsync();
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
        }
    }
}
