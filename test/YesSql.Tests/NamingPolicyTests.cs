using System.Data;
using System.Linq;
using System.Threading.Tasks;
using YesSql.Provider.Sqlite;
using YesSql.Sql;
using Xunit;

namespace YesSql.Tests
{
    public class NamingPolicyTests
    {
        [Fact]

        public async Task UseCustomNamingPolicy()
        {
            // Arrange
            var tempFolder = new TemporaryFolder();
            var connectionString = @"Data Source=" + tempFolder.Folder + "yessql.db;Cache=Shared";

            // Act & Assert
            var configuration = new Configuration()
                .WithNamingPolicy(new SnakeCaseNamingPolicy())
                .UseSqLite(connectionString)
                .UseDefaultIdGenerator();

            var store = await StoreFactory.CreateAsync(configuration);
            using (var connection = store.Configuration.ConnectionFactory.CreateConnection())
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction(store.Configuration.IsolationLevel))
                {
                    var builder = new SchemaBuilder(store.Configuration, transaction);
                    builder.CreateTable("UsersInRoles", column => column
                            .Column<int>("UserId")
                            .Column<int>("RoleId")
                        );
                    transaction.Commit();
                }

                var tableNames = GetTable(connection, "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%'")
                    .Rows.Cast<DataRow>()
                    .Select(r => r[0].ToString());
                
                Assert.True(tableNames.Count() > 0);
                Assert.Contains("users_in_roles", tableNames);
                
                var columnNames = GetTable(connection, $"SELECT * FROM users_in_roles")
                    .Columns.Cast<DataColumn>()
                    .Select(c => c.ColumnName);
                
                Assert.Equal("user_id", columnNames.First());
                Assert.Equal("role_id", columnNames.Last());
            }
        }

        private IConfiguration CreateConfiguration()
        {
            var tempFolder = new TemporaryFolder();
            var connectionString = @"Data Source=" + tempFolder.Folder + "yessql.db;Cache=Shared";
            var configuration = new Configuration
            {
                NamingPolicy = new SnakeCaseNamingPolicy()
            };

            return configuration
                .UseSqLite(connectionString)
                .UseDefaultIdGenerator();
        }

        private DataTable GetTable(IDbConnection connection, string sql)
        {
            var command = connection.CreateCommand();
            command.CommandText = sql;

            var table = new DataTable();
            table.Load(command.ExecuteReader());

            return table;
        }

        private class SnakeCaseNamingPolicy : NamingPolicy
        {
            public override string ConvertName(string name)
            {
                const string separator = "_";

                if (string.IsNullOrEmpty(name))
                {
                    return name;
                }

                return string.Concat(name.Select((c, i) => i > 0 && char.IsUpper(c) ? separator + c.ToString() : c.ToString())).ToLower();
            }
        }
    }
}
