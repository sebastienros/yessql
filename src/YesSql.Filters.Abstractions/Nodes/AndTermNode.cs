using System.Linq;
using YesSql.Filters.Services;

namespace YesSql.Filters.Nodes;

public class AndTermNode : CompoundTermNode
{
    public AndTermNode(TermOperationNode existingTerm, TermOperationNode newTerm) : base(existingTerm.TermName)
    {
        Children.Add(existingTerm);
        Children.Add(newTerm);
    }

    public override TResult Accept<TArgument, TResult>(IFilterVisitor<TArgument, TResult> visitor, TArgument argument)
        => visitor.Visit(this, argument);

    public override string ToNormalizedString()
        => string.Join(" ", Children.Select(c => c.ToNormalizedString()));

    public override string ToString()
        => string.Join(" ", Children.Select(c => c.ToString()));
}