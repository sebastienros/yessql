using YesSql.Filters.Services;

namespace YesSql.Filters.Nodes;

/// <summary>
/// Represents a unary operation that negates a single operand, for example a leading <c>NOT</c> or <c>!</c> applied to a value.
/// </summary>
public class NotUnaryNode : OperatorNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotUnaryNode"/> class.
    /// </summary>
    /// <param name="operatorValue">The operator text as it appeared in the source, for example <c>NOT</c> or <c>!</c>.</param>
    /// <param name="operation">The operand that is negated.</param>
    public NotUnaryNode(string operatorValue, UnaryNode operation)
    {
        OperatorValue = operatorValue;
        Operation = operation;
    }

    /// <summary>
    /// Gets the operator text as it appeared in the source.
    /// </summary>
    public string OperatorValue { get; }
    /// <summary>
    /// Gets the operand that is negated.
    /// </summary>
    public UnaryNode Operation { get; }

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
    /// Returns a normalized string representation combining the operator and the negated operand.
    /// </summary>
    /// <returns>The normalized string representation of the operation.</returns>
    public override string ToNormalizedString()
        => ToString();

    /// <summary>
    /// Returns the string representation combining the operator and the negated operand.
    /// </summary>
    /// <returns>The string representation of the operation.</returns>
    public override string ToString() => $"{OperatorValue} {Operation}";
}