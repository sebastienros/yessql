using Xunit;
using YesSql.Provider.Sqlite;
using YesSql.Sql;

namespace YesSql.Tests
{
    public class SqlBuilderTests
    {
        private static SqlBuilder CreateSelectBuilder()
        {
            var builder = new SqlBuilder("tp_", new SqliteDialect());
            builder.Select();
            builder.AddSelector("*");
            builder.Table("People", "p", null);

            return builder;
        }

        [Fact]
        public void CloneShouldPreserveDistinct()
        {
            var builder = CreateSelectBuilder();
            builder.Distinct();

            var clone = builder.Clone();

            Assert.Contains("DISTINCT", clone.ToSqlString());
        }

        [Fact]
        public void CloneShouldNotAddDistinctWhenSourceHasNone()
        {
            var builder = CreateSelectBuilder();

            var clone = builder.Clone();

            Assert.DoesNotContain("DISTINCT", clone.ToSqlString());
        }

        [Fact]
        public void CloneShouldBeIsolatedFromSource()
        {
            var builder = CreateSelectBuilder();

            var clone = builder.Clone();

            // Mutating the source after cloning must not affect the clone.
            builder.Distinct();

            Assert.DoesNotContain("DISTINCT", clone.ToSqlString());
            Assert.Contains("DISTINCT", builder.ToSqlString());
        }
    }
}
