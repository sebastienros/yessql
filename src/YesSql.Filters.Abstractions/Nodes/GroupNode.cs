using YesSql.Filters.Services;

namespace YesSql.Filters.Nodes;

/// <summary>
/// Marks a node as being produced by a group request, i.e. () were specified
/// </summary>
public class GroupNode : OperatorNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GroupNode"/> class.
    /// </summary>
    /// <param name="operation">The operation enclosed by the group.</param>
    public GroupNode(OperatorNode operation)
    {
        Operation = operation;
    }

    /// <summary>
    /// Gets the operation enclosed by the group.
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

    /// <summary>
    /// Returns a normalized string representation of the group, preserving the surrounding parenthesis.
    /// </summary>
    /// <returns>The normalized string representation of the group.</returns>
    public override string ToNormalizedString()
        => ToString();

    /// <summary>
    /// Returns the string representation of the group, including the surrounding parenthesis.
    /// </summary>
    /// <returns>The string representation of the group.</returns>
    public override string ToString() => $"({Operation})";
}