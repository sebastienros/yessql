namespace YesSql.Filters.Nodes;

public class DefaultTermNode : TermOperationNode
{
    public DefaultTermNode(string termName, OperatorNode operation) : base(termName, operation)
    {
    }

    public override string ToNormalizedString() // normalizing includes the term name even if not specified.
        => $"{TermName}:{Operation.ToNormalizedString()}";

    public override string ToString() => $"{Operation}";
}