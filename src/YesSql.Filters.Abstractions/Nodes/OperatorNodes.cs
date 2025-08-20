
using System;
using YesSql.Filters.Abstractions.Services;

namespace YesSql.Filters.Abstractions.Nodes
{
    public abstract class OperatorNode : FilterNode
    {
    }

    public enum OperateNodeQuotes
    {
        None,
        Double,
        Single
    }

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
        public bool HasValue => !string.IsNullOrEmpty(Value);

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
                    _ => throw new NotSupportedException()
                };
            }
            else
            {
                return string.Empty;
            }
        }

        public override TResult Accept<TArgument, TResult>(IFilterVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.Visit(this, argument);
    }

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

        public override string ToString()
            => $"{OperatorValue} {Operation.ToString()}";
    }

    public class OrNode : OperatorNode
    {
        public OrNode(OperatorNode left, OperatorNode right, string value)
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
            => $"({Left.ToNormalizedString()} OR {Right.ToNormalizedString()})";

        public override string ToString()
            => string.IsNullOrWhiteSpace(Value) ? $"{Left.ToString()} {Right.ToString()}" : $"{Left.ToString()} {Value} {Right.ToString()}";
    }

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

        public override string ToString()
            => $"{Left.ToString()} {Value} {Right.ToString()}";
    }

    public class NotNode : AndNode
    {
        public NotNode(OperatorNode left, OperatorNode right, string value) : base(left, right, value)
        {
        }

        public override string ToNormalizedString()
            => $"({Left.ToNormalizedString()} NOT {Right.ToNormalizedString()})";

        public override string ToString()
            => $"{Left.ToString()} {Value} {Right.ToString()}";
    }

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

        public override string ToString()
            => $"({Operation.ToString()})";
    }
}
