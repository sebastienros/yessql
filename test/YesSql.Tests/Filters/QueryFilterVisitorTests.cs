using System;
using Xunit;
using YesSql.Filters.Nodes;
using YesSql.Filters.Query.Services;
using YesSql.Tests.Models;

namespace YesSql.Tests.Filters
{
    public class QueryFilterVisitorTests
    {
        [Fact]
        public void ShouldThrowWhenMatchPredicateIsMissing()
        {
            var visitor = new QueryFilterVisitor<Person>();
            var context = new QueryExecutionContext<Person>(null)
            {
                // A term option with no match predicate.
                CurrentTermOption = new QueryTermOption<Person>("name", matchPredicate: null)
            };

            var node = new UnaryNode("steve", OperateNodeQuotes.None, useMatch: true);

            Assert.Throws<InvalidOperationException>(() => visitor.Visit(node, context));
        }

        [Fact]
        public void ShouldThrowWhenNotMatchPredicateIsMissing()
        {
            var visitor = new QueryFilterVisitor<Person>();
            var context = new QueryExecutionContext<Person>(null)
            {
                // A term option with a match predicate but no negated predicate.
                CurrentTermOption = new QueryTermOption<Person>(
                    "name",
                    matchPredicate: (value, query, ctx) => new System.Threading.Tasks.ValueTask<IQuery<Person>>(query),
                    notMatchPredicate: null)
            };

            var node = new UnaryNode("steve", OperateNodeQuotes.None, useMatch: false);

            Assert.Throws<InvalidOperationException>(() => visitor.Visit(node, context));
        }

        [Fact]
        public void ShouldNotThrowWhenMatchPredicateIsPresent()
        {
            var visitor = new QueryFilterVisitor<Person>();
            var context = new QueryExecutionContext<Person>(null)
            {
                CurrentTermOption = new QueryTermOption<Person>(
                    "name",
                    matchPredicate: (value, query, ctx) => new System.Threading.Tasks.ValueTask<IQuery<Person>>(query))
            };

            var node = new UnaryNode("steve", OperateNodeQuotes.None, useMatch: true);

            // Visiting builds the predicate delegate without evaluating it; it must not throw.
            var result = visitor.Visit(node, context);

            Assert.NotNull(result);
        }
    }
}
