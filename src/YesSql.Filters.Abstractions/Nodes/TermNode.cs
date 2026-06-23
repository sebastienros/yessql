using YesSql.Filters.Services;

namespace YesSql.Filters.Nodes
{
    /// <summary>
    /// Represents the base class for a named term in a filter expression, identifying the field or facet the filter applies to.
    /// </summary>
    public abstract class TermNode : FilterNode
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TermNode"/> class.
        /// </summary>
        /// <param name="termName">The name of the term.</param>
        protected TermNode(string termName)
        {
            TermName = termName;
        }

        /// <summary>
        /// Gets the name of the term.
        /// </summary>
        public string TermName { get; }
        /// <summary>
        /// Accepts a visitor and dispatches to the appropriate visit method for this node type.
        /// </summary>
        /// <typeparam name="TArgument">The type of the argument passed to the visitor.</typeparam>
        /// <typeparam name="TResult">The type of the result produced by the visitor.</typeparam>
        /// <param name="visitor">The visitor to accept.</param>
        /// <param name="argument">The argument passed to the visitor.</param>
        /// <returns>The result produced by the visitor.</returns>
        public override TResult Accept<TArgument, TResult>(IFilterVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.Visit(this, argument);
    }
}
