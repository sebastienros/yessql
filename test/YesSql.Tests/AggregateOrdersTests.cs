using System.Collections.Generic;
using System.Linq;
using Xunit;
using YesSql.Provider.SqlServer;

namespace YesSql.Tests
{
    // Covers BaseDialect.GetAggregateOrders, which is used by the SqlServer, PostgreSql and MySql
    // dialects (Sqlite overrides it). These run with no database connection.
    public class AggregateOrdersTests
    {
        [Fact]
        public void ShouldAggregateOrderedFields()
        {
            var dialect = new SqlServerDialect();

            var result = dialect.GetAggregateOrders(new List<string> { "[col]" }, new List<string> { "[col]" }).ToList();

            var (aggregate, alias) = Assert.Single(result);
            Assert.Equal("MAX([col]) AS [order_1]", aggregate);
            Assert.Equal("[order_1]", alias);
        }

        [Fact]
        public void ShouldAttachDirectionToAlias()
        {
            var dialect = new SqlServerDialect();

            var result = dialect.GetAggregateOrders(new List<string> { "[col]" }, new List<string> { "[col]", "DESC" }).ToList();

            var (aggregate, alias) = Assert.Single(result);
            Assert.Equal("MAX([col]) AS [order_1]", aggregate);
            Assert.Equal("[order_1] DESC", alias);
        }

        [Fact]
        public void ShouldSkipPunctuationAndTrimWhitespace()
        {
            var dialect = new SqlServerDialect();

            // Order segments can contain surrounding whitespace and comma separators that must be ignored.
            var orderBy = new List<string> { " [a] ", " , ", " [b] ", " ASC " };
            var result = dialect.GetAggregateOrders(new List<string> { "[a]", "[b]" }, orderBy).ToList();

            Assert.Equal(2, result.Count);
            Assert.Equal("MAX( [a] ) AS [order_1]", result[0].aggregate);
            Assert.Equal("[order_1]", result[0].alias);
            Assert.Equal("MAX( [b] ) AS [order_3]", result[1].aggregate);
            Assert.Equal("[order_3] ASC", result[1].alias);
        }
    }
}
