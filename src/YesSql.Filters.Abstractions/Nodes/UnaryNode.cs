using System;
using YesSql.Filters.Services;

namespace YesSql.Filters.Nodes;

/// <summary>
/// Represents a single operand value in a filter expression, optionally quoted.
/// </summary>
public class UnaryNode : OperatorNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnaryNode"/> class.
    /// </summary>
    /// <param name="value">The operand value.</param>
    /// <param name="quotes">The kind of quoting that surrounded the value in the source.</param>
    /// <param name="useMatch">Whether the value should be treated as a match expression rather than a negation or exclusion.</param>
    public UnaryNode(string value, OperateNodeQuotes quotes, bool useMatch = true)
    {
        Value = value;
        Quotes = quotes;
        UseMatch = useMatch;
    }

    /// <summary>
    /// Gets the operand value.
    /// </summary>
    public string Value { get; }
    /// <summary>
    /// Gets the kind of quoting that surrounded the value in the source.
    /// </summary>
    public OperateNodeQuotes Quotes { get; }
    /// <summary>
    /// Gets a value indicating whether the value should be treated as a match expression.
    /// </summary>
    public bool UseMatch { get; }
    /// <summary>
    /// Gets a value indicating whether the node has a non-empty value.
    /// </summary>
    public bool HasValue => !string.IsNullOrEmpty(Value);

    /// <summary>
    /// Returns a normalized string representation of the value, preserving any quoting.
    /// </summary>
    /// <returns>The normalized string representation of the value.</returns>
    public override string ToNormalizedString()
        => ToString();

    /// <summary>
    /// Returns the string representation of the value, re-applying the original quotes when present.
    /// </summary>
    /// <returns>The quoted value, or an empty string when the node has no value.</returns>
    public override string ToString()
    {
        if (HasValue)
        {
            return Quotes switch
            {
                OperateNodeQuotes.None => Value,
                OperateNodeQuotes.Double => $"\"{Value}\"",
                OperateNodeQuotes.Single => $"\'{Value}\'",
                _ => throw new NotSupportedException()
            };
        }
        else
        {
            return string.Empty;
        }
    }

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