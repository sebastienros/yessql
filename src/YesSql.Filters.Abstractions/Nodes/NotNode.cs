namespace YesSql.Filters.Nodes;

public class NotNode : AndNode
{
    public NotNode(OperatorNode left, OperatorNode right, string value) : base(left, right, value)
    {
    }

    public override string ToNormalizedString()
        => $"({Left.ToNormalizedString()} NOT {Right.ToNormalizedString()})";

    public override string ToString() => $"{Left} {Value} {Right}";
}