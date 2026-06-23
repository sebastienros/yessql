using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YesSql.Filters.Nodes;
using YesSql.Filters.Services;

namespace YesSql.Filters.Query.Services
{
    /// <summary>
    /// Visits the nodes of a parsed query filter and builds the predicates applied to an <see cref="IQuery{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the queried document.</typeparam>
    public class QueryFilterVisitor<T> : IFilterVisitor<QueryExecutionContext<T>, Func<IQuery<T>, ValueTask<IQuery<T>>>> where T : class
    {
        /// <summary>
        /// Visits a <see cref="TermOperationNode"/> and returns the predicate to apply.
        /// </summary>
        /// <param name="node">The node to visit.</param>
        /// <param name="argument">The current execution context.</param>
        /// <returns>The predicate to apply to the query.</returns>
        public Func<IQuery<T>, ValueTask<IQuery<T>>> Visit(TermOperationNode node, QueryExecutionContext<T> argument)
            => node.Operation.Accept(this, argument);

        /// <summary>
        /// Visits an <see cref="AndTermNode"/> and returns the predicate to apply.
        /// </summary>
        /// <param name="node">The node to visit.</param>
        /// <param name="argument">The current execution context.</param>
        /// <returns>The predicate to apply to the query.</returns>
        public Func<IQuery<T>, ValueTask<IQuery<T>>> Visit(AndTermNode node, QueryExecutionContext<T> argument)
        {
            var predicates = new List<Func<IQuery<T>, ValueTask<IQuery<T>>>>();
            foreach (var child in node.Children)
            {
                Func<IQuery<T>, ValueTask<IQuery<T>>> predicate = (q) => argument.Item.AllAsync(
                    (q) => child.Operation.Accept(this, argument)(q)
                );
                predicates.Add(predicate);
            }

            var result = (Func<IQuery<T>, ValueTask<IQuery<T>>>)Delegate.Combine(predicates.ToArray());

            return result;
        }

        /// <summary>
        /// Visits a <see cref="UnaryNode"/> and returns the predicate to apply.
        /// </summary>
        /// <param name="node">The node to visit.</param>
        /// <param name="argument">The current execution context.</param>
        /// <returns>The predicate to apply to the query.</returns>
        public Func<IQuery<T>, ValueTask<IQuery<T>>> Visit(UnaryNode node, QueryExecutionContext<T> argument)
        {
            var currentQuery = argument.CurrentTermOption.MatchPredicate;
            if (!node.UseMatch)
            {
                currentQuery = argument.CurrentTermOption.NotMatchPredicate;
            }

            if (currentQuery == null)
            {
                throw new InvalidOperationException(
                    "The term does not define a "
                    + (node.UseMatch ? "match" : "negated (NOT) match")
                    + " predicate.");
            }

            return result => currentQuery(node.Value, argument.Item, argument);
        }

        /// <summary>
        /// Visits a <see cref="NotUnaryNode"/> and returns the predicate to apply.
        /// </summary>
        /// <param name="node">The node to visit.</param>
        /// <param name="argument">The current execution context.</param>
        /// <returns>The predicate to apply to the query.</returns>
        public Func<IQuery<T>, ValueTask<IQuery<T>>> Visit(NotUnaryNode node, QueryExecutionContext<T> argument)
        {
            return result => argument.Item.AllAsync(
                 (q) => node.Operation.Accept(this, argument)(q)
            );
        }

        /// <summary>
        /// Visits an <see cref="OrNode"/> and returns the predicate to apply.
        /// </summary>
        /// <param name="node">The node to visit.</param>
        /// <param name="argument">The current execution context.</param>
        /// <returns>The predicate to apply to the query.</returns>
        public Func<IQuery<T>, ValueTask<IQuery<T>>> Visit(OrNode node, QueryExecutionContext<T> argument)
        {
            return result => argument.Item.AnyAsync(
                (q) => node.Left.Accept(this, argument)(q),
                (q) => node.Right.Accept(this, argument)(q)
            );
        }

        /// <summary>
        /// Visits an <see cref="AndNode"/> and returns the predicate to apply.
        /// </summary>
        /// <param name="node">The node to visit.</param>
        /// <param name="argument">The current execution context.</param>
        /// <returns>The predicate to apply to the query.</returns>
        public Func<IQuery<T>, ValueTask<IQuery<T>>> Visit(AndNode node, QueryExecutionContext<T> argument)
        {
            return result => argument.Item.AllAsync(
                (q) => node.Left.Accept(this, argument)(q),
                (q) => node.Right.Accept(this, argument)(q)
            );
        }

        /// <summary>
        /// Visits a <see cref="GroupNode"/> and returns the predicate to apply.
        /// </summary>
        /// <param name="node">The node to visit.</param>
        /// <param name="argument">The current execution context.</param>
        /// <returns>The predicate to apply to the query.</returns>
        public Func<IQuery<T>, ValueTask<IQuery<T>>> Visit(GroupNode node, QueryExecutionContext<T> argument)
            => node.Operation.Accept(this, argument);

        /// <summary>
        /// Visits a <see cref="TermNode"/> and returns the predicate to apply.
        /// </summary>
        /// <param name="node">The node to visit.</param>
        /// <param name="argument">The current execution context.</param>
        /// <returns>The predicate to apply to the query.</returns>
        public Func<IQuery<T>, ValueTask<IQuery<T>>> Visit(TermNode node, QueryExecutionContext<T> argument)
            => node.Accept(this, argument);
    }
}
