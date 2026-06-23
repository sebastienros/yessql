namespace YesSql.Filters.Nodes;

/// <summary>
/// Represents a term that was not explicitly named in the filter expression and is associated with a default term name.
/// </summary>
public class DefaultTermNode : TermOperationNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultTermNode"/> class.
    /// </summary>
    /// <param name="termName">The default name assigned to the term.</param>
    /// <param name="operation">The operation applied to the term.</param>
    public DefaultTermNode(string termName, OperatorNode operation) : base(termName, operation)
    {
    }

    /// <summary>
    /// Returns a normalized string representation of the term, including the default term name even though it was not specified.
    /// </summary>
    /// <returns>The normalized string in the form <c>name:value</c>.</returns>
    public override string ToNormalizedString() // normalizing includes the term name even if not specified.
        => $"{TermName}:{Operation.ToNormalizedString()}";

    /// <summary>
    /// Returns the string representation of the term, containing only the operation value as it was originally written.
    /// </summary>
    /// <returns>The string representation of the operation.</returns>
    public override string ToString() => $"{Operation}";
}