using Xunit;
using YesSql.Tests.Fixtures;

namespace YesSql.Tests
{
    /// <summary>
    /// Collection definitions to group tests by database type.
    /// Tests in the same collection share a container fixture and run sequentially.
    /// </summary>
    /// 
    [CollectionDefinition("MySql", DisableParallelization = true)]
    public class MySqlCollection : ICollectionFixture<MySqlContainerFixture>
    {
    }

    [CollectionDefinition("PostgreSql", DisableParallelization = true)]
    public class PostgreSqlCollection : ICollectionFixture<PostgreSqlContainerFixture>
    {
    }

    [CollectionDefinition("SqlServer2017", DisableParallelization = true)]
    public class SqlServer2017Collection : ICollectionFixture<SqlServer2017ContainerFixture>
    {
    }

    [CollectionDefinition("SqlServer2019", DisableParallelization = true)]
    public class SqlServer2019Collection : ICollectionFixture<SqlServer2019ContainerFixture>
    {
    }
}
