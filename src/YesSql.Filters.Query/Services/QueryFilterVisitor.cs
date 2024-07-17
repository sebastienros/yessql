using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YesSql.Filters.Abstractions.Nodes;
using YesSql.Filters.Abstractions.Services;

namespace YesSql.Filters.Query.Services
{
    public class QueryFilterVisitor<T> : IFilterVisitor<QueryExecutionContext<T>, Func<IQuery<T>, ValueTask<IQuery<T>>>> where T : class
    {
        public Func<IQuery<T>, ValueTask<IQuery<T>>> Visit(TermOperationNode node, QueryExecutionContext<T> argument)
            => node.Operation.Accept(this, argument);

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

        public Func<IQuery<T>, ValueTask<IQuery<T>>> Visit(UnaryNode node, QueryExecutionContext<T> argument)
        {
            var currentQuery = argument.CurrentTermOption.MatchPredicate;
            if (!node.UseMatch)
            {
                currentQuery = argument.CurrentTermOption.NotMatchPredicate;
            }

            return result => currentQuery(node.Value, argument.Item, argument);
        }

        public Func<IQuery<T>, ValueTask<IQuery<T>>> Visit(NotUnaryNode node, QueryExecutionContext<T> argument)
            => result => argument.Item.AllAsync(
                 (q) => node.Operation.Accept(this, argument)(q)
            );
        

        public Func<IQuery<T>, ValueTask<IQuery<T>>> Visit(OrNode node, QueryExecutionContext<T> argument)
            => result => argument.Item.AnyAsync(
                (q) => node.Left.Accept(this, argument)(q),
                (q) => node.Right.Accept(this, argument)(q)
            );
        

        public Func<IQuery<T>, ValueTask<IQuery<T>>> Visit(AndNode node, QueryExecutionContext<T> argument)
           => result => argument.Item.AllAsync(
                (q) => node.Left.Accept(this, argument)(q),
                (q) => node.Right.Accept(this, argument)(q)
            );
        

        public Func<IQuery<T>, ValueTask<IQuery<T>>> Visit(GroupNode node, QueryExecutionContext<T> argument)
            => node.Operation.Accept(this, argument);

        public Func<IQuery<T>, ValueTask<IQuery<T>>> Visit(TermNode node, QueryExecutionContext<T> argument)
            => node.Accept(this, argument);
    }
}
