using YesSql.Filters.Nodes;

namespace YesSql.Filters.Services
{
    /// <summary>
    /// Defines a visitor that traverses the nodes of a parsed filter expression tree and produces a result for each node type.
    /// </summary>
    /// <typeparam name="TArgument">The type of the argument passed through the traversal.</typeparam>
    /// <typeparam name="TResult">The type of the result produced for each node.</typeparam>
    public interface IFilterVisitor<TArgument, TResult>
    {
        /// <summary>
        /// Visits a <see cref="TermNode"/>.
        /// </summary>
        /// <param name="node">The node to visit.</param>
        /// <param name="argument">The argument passed through the traversal.</param>
        /// <returns>The result produced for the node.</returns>
        TResult Visit(TermNode node, TArgument argument);
        /// <summary>
        /// Visits a <see cref="TermOperationNode"/>.
        /// </summary>
        /// <param name="node">The node to visit.</param>
        /// <param name="argument">The argument passed through the traversal.</param>
        /// <returns>The result produced for the node.</returns>
        TResult Visit(TermOperationNode node, TArgument argument);
        /// <summary>
        /// Visits an <see cref="AndTermNode"/>.
        /// </summary>
        /// <param name="node">The node to visit.</param>
        /// <param name="argument">The argument passed through the traversal.</param>
        /// <returns>The result produced for the node.</returns>
        TResult Visit(AndTermNode node, TArgument argument);
        /// <summary>
        /// Visits a <see cref="UnaryNode"/>.
        /// </summary>
        /// <param name="node">The node to visit.</param>
        /// <param name="argument">The argument passed through the traversal.</param>
        /// <returns>The result produced for the node.</returns>
        TResult Visit(UnaryNode node, TArgument argument);
        /// <summary>
        /// Visits a <see cref="NotUnaryNode"/>.
        /// </summary>
        /// <param name="node">The node to visit.</param>
        /// <param name="argument">The argument passed through the traversal.</param>
        /// <returns>The result produced for the node.</returns>
        TResult Visit(NotUnaryNode node, TArgument argument);
        /// <summary>
        /// Visits an <see cref="OrNode"/>.
        /// </summary>
        /// <param name="node">The node to visit.</param>
        /// <param name="argument">The argument passed through the traversal.</param>
        /// <returns>The result produced for the node.</returns>
        TResult Visit(OrNode node, TArgument argument);
        /// <summary>
        /// Visits an <see cref="AndNode"/>.
        /// </summary>
        /// <param name="node">The node to visit.</param>
        /// <param name="argument">The argument passed through the traversal.</param>
        /// <returns>The result produced for the node.</returns>
        TResult Visit(AndNode node, TArgument argument);
        /// <summary>
        /// Visits a <see cref="GroupNode"/>.
        /// </summary>
        /// <param name="node">The node to visit.</param>
        /// <param name="argument">The argument passed through the traversal.</param>
        /// <returns>The result produced for the node.</returns>
        TResult Visit(GroupNode node, TArgument argument);
    }
}
