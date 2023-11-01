using YesSql.Filters.Services;

namespace YesSql.Filters.Nodes;

public class NotUnaryNode : OperatorNode
{
    public NotUnaryNode(string operatorValue, UnaryNode operation)
    {
        OperatorValue = operatorValue;
        Operation = operation;
    }

    public string OperatorValue { get; }
    public UnaryNode Operation { get; }

    public override TResult Accept<TArgument, TResult>(IFilterVisitor<TArgument, TResult> visitor, TArgument argument)
        => visitor.Visit(this, argument);
    public override string ToNormalizedString()
        => ToString();

    public override string ToString() => $"{OperatorValue} {Operation}";
}