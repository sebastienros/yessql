namespace YesSql.Filters.Nodes;

/// <summary>
/// Represents a boolean <c>NOT</c> operation that excludes the right operand from the result of the left operand.
/// </summary>
public class NotNode : AndNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotNode"/> class.
    /// </summary>
    /// <param name="left">The left operand of the operation.</param>
    /// <param name="right">The right operand that is negated.</param>
    /// <param name="value">The operator text as it appeared in the source, for example <c>NOT</c> or <c>!</c>.</param>
    public NotNode(OperatorNode left, OperatorNode right, string value) : base(left, right, value)
    {
    }

    /// <summary>
    /// Returns a normalized string representation of the operation using the canonical <c>NOT</c> operator and explicit parenthesis.
    /// </summary>
    /// <returns>The normalized string representation of the operation.</returns>
    public override string ToNormalizedString()
        => $"({Left.ToNormalizedString()} NOT {Right.ToNormalizedString()})";

    /// <summary>
    /// Returns the string representation of the operation using the original operator text.
    /// </summary>
    /// <returns>The string representation of the operation.</returns>
    public override string ToString() => $"{Left} {Value} {Right}";
}