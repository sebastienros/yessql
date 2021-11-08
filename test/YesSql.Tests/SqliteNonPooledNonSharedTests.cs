using Xunit.Abstractions;
using YesSql.Provider.Sqlite;

namespace YesSql.Tests
{
    /// <summary>
    /// Run all tests with a Sqlite document storage
    /// </summary>
    public class SqliteNonPooledNonSharedTests : SqliteTests
    {
        private TemporaryFolder _tempFolder;

        protected override string DecimalColumnDefinitionFormatString => "NUMERIC";

        public SqliteNonPooledNonSharedTests(ITestOutputHelper output) : base(output)
        {
        }

        protected override IConfiguration CreateConfiguration()
        {
            _tempFolder = new TemporaryFolder();
            var connectionString = @"Data Source=" + _tempFolder.Folder + "yessql.db;Pooling=False";

            return new Configuration()
                .UseSqLite(connectionString)
                .SetTablePrefix(TablePrefix)
                .UseDefaultIdGenerator()
                ;
        }
    }
}
