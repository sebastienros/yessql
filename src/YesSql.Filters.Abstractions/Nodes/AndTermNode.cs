using System.Linq;
using YesSql.Filters.Services;

namespace YesSql.Filters.Nodes;

/// <summary>
/// Represents two or more operations on the same term combined together, for example when a term is specified multiple times.
/// </summary>
public class AndTermNode : CompoundTermNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AndTermNode"/> class from an existing term operation and a newly parsed one.
    /// </summary>
    /// <param name="existingTerm">The previously parsed operation for the term.</param>
    /// <param name="newTerm">The newly parsed operation for the term.</param>
    public AndTermNode(TermOperationNode existingTerm, TermOperationNode newTerm) : base(existingTerm.TermName)
    {
        Children.Add(existingTerm);
        Children.Add(newTerm);
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

    /// <summary>
    /// Returns a normalized string representation that joins the normalized form of each child operation with a space.
    /// </summary>
    /// <returns>The normalized string representation of the compound term.</returns>
    public override string ToNormalizedString()
        => string.Join(" ", Children.Select(c => c.ToNormalizedString()));

    /// <summary>
    /// Returns a string representation that joins each child operation with a space.
    /// </summary>
    /// <returns>The string representation of the compound term.</returns>
    public override string ToString()
        => string.Join(" ", Children.Select(c => c.ToString()));
}