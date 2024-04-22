using System.Threading.Tasks;
using Testcontainers.MsSql;
using Xunit.Abstractions;
using YesSql.Provider.SqlServer;

namespace YesSql.Tests
{
    public class SqlServer2017Tests : SqlServerTests
    {
        private readonly MsSqlContainer _sqlServerContainer = new MsSqlBuilder().WithImage("mcr.microsoft.com/mssql/server:2017-latest").Build();

        public SqlServer2017Tests(ITestOutputHelper output) : base(output)
        {
        }

        public override string ConnectionString => _sqlServerContainer.GetConnectionString();

        public override async Task InitializeAsync()
        {
            await _sqlServerContainer.StartAsync();
            await base.InitializeAsync();
        }

        protected override IConfiguration CreateConfiguration()
        {
            return new Configuration()
                .UseSqlServer(ConnectionString, "BobaFett")
                .SetTablePrefix(TablePrefix)
                .UseBlockIdGenerator()
                .SetIdentityColumnSize(IdentityColumnSize.Int64)
                ;
        }

        public override async Task DisposeAsync() => await _sqlServerContainer.DisposeAsync();
    }
}
