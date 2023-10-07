using System;
using YesSql.Filters.Services;

namespace YesSql.Filters.Nodes;

public class UnaryNode : OperatorNode
{
    public UnaryNode(string value, OperateNodeQuotes quotes, bool useMatch = true)
    {
        Value = value;
        Quotes = quotes;
        UseMatch = useMatch;
    }

    public string Value { get; }
    public OperateNodeQuotes Quotes { get; }
    public bool UseMatch { get; }
    public bool HasValue => !String.IsNullOrEmpty(Value);

    public override string ToNormalizedString()
        => ToString();

    public override string ToString()
    {
        if (HasValue)
        {
            return Quotes switch
            {
                OperateNodeQuotes.None => Value,
                OperateNodeQuotes.Double => $"\"{Value}\"",
                OperateNodeQuotes.Single => $"\'{Value}\'",
                _ => throw new NotImplementedException()
            };
        }
        else
        {
            return String.Empty;
        }
    }

    public override TResult Accept<TArgument, TResult>(IFilterVisitor<TArgument, TResult> visitor, TArgument argument)
        => visitor.Visit(this, argument);
}