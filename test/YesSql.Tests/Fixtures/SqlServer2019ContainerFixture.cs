namespace YesSql.Tests.Fixtures
{
    /// <summary>
    /// Fixture that manages a SQL Server 2019 container shared across all tests in a test class.
    /// </summary>
    public class SqlServer2019ContainerFixture : SqlServerContainerFixture
    {
        protected override string DockerImage => "mcr.microsoft.com/mssql/server:2019-latest";
    }
}
