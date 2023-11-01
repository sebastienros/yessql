using YesSql.Filters.Services;

namespace YesSql.Filters.Nodes;

/// <summary>
/// Marks a node as being produced by a group request, i.e. () were specified
/// </summary>
public class GroupNode : OperatorNode
{
    public GroupNode(OperatorNode operation)
    {
        Operation = operation;
    }

    public OperatorNode Operation { get; }

    public override TResult Accept<TArgument, TResult>(IFilterVisitor<TArgument, TResult> visitor, TArgument argument)
        => visitor.Visit(this, argument);

    public override string ToNormalizedString()
        => ToString();

    public override string ToString() => $"({Operation})";
}