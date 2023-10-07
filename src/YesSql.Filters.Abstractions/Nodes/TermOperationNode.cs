using YesSql.Filters.Services;

namespace YesSql.Filters.Nodes;

public abstract class TermOperationNode : TermNode
{
    protected TermOperationNode(string termName, OperatorNode operation) : base(termName)
    {
        Operation = operation;
    }

    public OperatorNode Operation { get; }

    public override TResult Accept<TArgument, TResult>(IFilterVisitor<TArgument, TResult> visitor, TArgument argument)
        => visitor.Visit(this, argument);
}