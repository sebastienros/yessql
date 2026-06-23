using YesSql.Filters.Services;

namespace YesSql.Filters.Nodes;

/// <summary>
/// Represents a boolean <c>OR</c> operation between two operands in a filter expression. This is also the default operation when operands are separated only by whitespace.
/// </summary>
public class OrNode : OperatorNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrNode"/> class.
    /// </summary>
    /// <param name="left">The left operand of the operation.</param>
    /// <param name="right">The right operand of the operation.</param>
    /// <param name="value">The operator text as it appeared in the source, for example <c>OR</c> or <c>||</c>, or an empty value when the default whitespace operator was used.</param>
    public OrNode(OperatorNode left, OperatorNode right, string value)
    {
        Left = left;
        Right = right;
        Value = value;
    }

    /// <summary>
    /// Gets the left operand of the operation.
    /// </summary>
    public OperatorNode Left { get; }
    /// <summary>
    /// Gets the right operand of the operation.
    /// </summary>
    public OperatorNode Right { get; }
    /// <summary>
    /// Gets the operator text as it appeared in the source.
    /// </summary>
    public string Value { get; }

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
    /// Returns a normalized string representation of the operation using the canonical <c>OR</c> operator and explicit parenthesis.
    /// </summary>
    /// <returns>The normalized string representation of the operation.</returns>
    public override string ToNormalizedString()
        => $"({Left.ToNormalizedString()} OR {Right.ToNormalizedString()})";

    /// <summary>
    /// Returns the string representation of the operation, omitting the operator text when the default whitespace operator was used.
    /// </summary>
    /// <returns>The string representation of the operation.</returns>
    public override string ToString()
        => string.IsNullOrWhiteSpace(Value) ? $"{Left} {Right}" : $"{Left} {Value} {Right}";
}