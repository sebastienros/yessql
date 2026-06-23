using YesSql.Filters.Services;

namespace YesSql.Filters.Nodes;

/// <summary>
/// Represents a term that applies an operation, such as a comparison or boolean expression, to a named term.
/// </summary>
public abstract class TermOperationNode : TermNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TermOperationNode"/> class.
    /// </summary>
    /// <param name="termName">The name of the term.</param>
    /// <param name="operation">The operation applied to the term.</param>
    protected TermOperationNode(string termName, OperatorNode operation) : base(termName)
    {
        Operation = operation;
    }

    /// <summary>
    /// Gets the operation applied to the term.
    /// </summary>
    public OperatorNode Operation { get; }

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