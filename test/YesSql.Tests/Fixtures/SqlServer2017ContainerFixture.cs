namespace YesSql.Tests.Fixtures
{
    /// <summary>
    /// Fixture that manages a SQL Server 2017 container shared across all tests in a test class.
    /// </summary>
    public class SqlServer2017ContainerFixture : SqlServerContainerFixture
    {
        protected override string DockerImage => "mcr.microsoft.com/mssql/server:2017-latest";
    }
}
