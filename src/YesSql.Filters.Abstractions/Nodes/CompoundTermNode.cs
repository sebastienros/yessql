using System.Collections.Generic;

namespace YesSql.Filters.Nodes;

public abstract class CompoundTermNode : TermNode
{
    protected CompoundTermNode(string termName) : base(termName)
    {
    }

    public List<TermOperationNode> Children { get; } = new List<TermOperationNode>();
}