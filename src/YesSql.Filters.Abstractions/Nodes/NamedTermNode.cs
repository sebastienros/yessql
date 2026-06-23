namespace YesSql.Filters.Nodes;

public class NamedTermNode : TermOperationNode
{
    public NamedTermNode(string termName, OperatorNode operation) : base(termName, operation)
    {
    }

    public override string ToNormalizedString() => $"{TermName}:{Operation.ToNormalizedString()}";

    public override string ToString() => $"{TermName}:{Operation}";
}