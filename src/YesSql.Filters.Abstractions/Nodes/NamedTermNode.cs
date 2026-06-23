namespace YesSql.Filters.Nodes;

/// <summary>
/// Represents a term that is explicitly named in the filter expression using the <c>name:value</c> syntax.
/// </summary>
public class NamedTermNode : TermOperationNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NamedTermNode"/> class.
    /// </summary>
    /// <param name="termName">The name of the term.</param>
    /// <param name="operation">The operation applied to the term.</param>
    public NamedTermNode(string termName, OperatorNode operation) : base(termName, operation)
    {
    }

    /// <summary>
    /// Returns a normalized string representation of the term, including its name.
    /// </summary>
    /// <returns>The normalized string in the form <c>name:value</c>.</returns>
    public override string ToNormalizedString() => $"{TermName}:{Operation.ToNormalizedString()}";

    /// <summary>
    /// Returns the string representation of the term, including its name.
    /// </summary>
    /// <returns>The string in the form <c>name:value</c>.</returns>
    public override string ToString() => $"{TermName}:{Operation}";
}