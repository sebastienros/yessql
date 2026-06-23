using System.Collections.Generic;

namespace YesSql.Filters.Nodes;

/// <summary>
/// Represents a term that aggregates multiple operations sharing the same term name into a single compound node.
/// </summary>
public abstract class CompoundTermNode : TermNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundTermNode"/> class.
    /// </summary>
    /// <param name="termName">The name of the term.</param>
    protected CompoundTermNode(string termName) : base(termName)
    {
    }

    /// <summary>
    /// Gets the operations that compose this term.
    /// </summary>
    public List<TermOperationNode> Children { get; } = new List<TermOperationNode>();
}