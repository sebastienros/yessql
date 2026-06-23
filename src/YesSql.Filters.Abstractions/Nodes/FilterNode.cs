using YesSql.Filters.Services;

namespace YesSql.Filters.Nodes
{
    /// <summary>
    /// Represents the base class for all nodes in a parsed filter expression tree.
    /// </summary>
    public abstract class FilterNode
    {
        /// <summary>
        /// Returns a normalized string representation of the node, applying canonical boolean logic and formatting.
        /// </summary>
        /// <returns>The normalized string representation of the node.</returns>
        public abstract string ToNormalizedString();

        /// <summary>
        /// Accepts a visitor and dispatches to the appropriate visit method for this node type.
        /// </summary>
        /// <typeparam name="TArgument">The type of the argument passed to the visitor.</typeparam>
        /// <typeparam name="TResult">The type of the result produced by the visitor.</typeparam>
        /// <param name="visitor">The visitor to accept.</param>
        /// <param name="argument">The argument passed to the visitor.</param>
        /// <returns>The result produced by the visitor.</returns>
        public abstract TResult Accept<TArgument, TResult>(IFilterVisitor<TArgument, TResult> visitor, TArgument argument);
    }
}
