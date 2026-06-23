using YesSql.Filters.Services;

namespace YesSql.Filters.Nodes;

public class AndNode : OperatorNode
{
    public AndNode(OperatorNode left, OperatorNode right, string value)
    {
        Left = left;
        Right = right;
        Value = value;
    }

    public OperatorNode Left { get; }
    public OperatorNode Right { get; }
    public string Value { get; }

    public override TResult Accept<TArgument, TResult>(IFilterVisitor<TArgument, TResult> visitor, TArgument argument)
        => visitor.Visit(this, argument);
    public override string ToNormalizedString()
        => $"({Left.ToNormalizedString()} AND {Right.ToNormalizedString()})";

    public override string ToString() => $"{Left} {Value} {Right}";
}