using Xunit.Abstractions;
using YesSql.Provider.Sqlite;

namespace YesSql.Tests
{
    /// <summary>
    /// Run all tests with a Sqlite document storage
    /// </summary>
    public class SqlitePooledNonSharedTests : SqliteTests
    {
        private TemporaryFolder _tempFolder;

        protected override string DecimalColumnDefinitionFormatString => "NUMERIC";

        public SqlitePooledNonSharedTests(ITestOutputHelper output) : base(output)
        {
        }

        protected override IConfiguration CreateConfiguration()
        {
            _tempFolder = new TemporaryFolder();
            var connectionString = @"Data Source=" + _tempFolder.Folder + "yessql.db;Pooling=True";

            return new Configuration()
                .UseSqLite(connectionString)
                .SetTablePrefix(TablePrefix)
                .UseDefaultIdGenerator()
                ;
        }
    }
}
