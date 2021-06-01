using System;
using System.Linq;
using Xunit;
using YesSql.Filters.Query;
using YesSql.Services;
using YesSql.Tests.Indexes;
using YesSql.Tests.Models;
using Parlot.Fluent;
using static Parlot.Fluent.Parsers;
using Parlot;

namespace YesSql.Tests.Filters
{
    
    public abstract class Node
    {

        public string Operator { get; set; }
    }

    public class DateNode : Node
    {
        public DateTime? DateTime { get; set; }

        public override string ToString()
            => $"{(String.IsNullOrEmpty(Operator) ? String.Empty : Operator)}{(DateTime.HasValue ? DateTime.Value.ToString("o") : String.Empty)}";
    }

    public class NowNode : Node
    {
        public long? Arithmetic { get; set; }

        public override string ToString()
            => $"{(String.IsNullOrEmpty(Operator) ? String.Empty : Operator)}@now{(Arithmetic.HasValue ? Arithmetic.Value.ToString() : String.Empty)}";
    }

    public abstract class ExpressionNode { }
    public class UnaryExpressionNode : ExpressionNode
    {
        public UnaryExpressionNode(Node node)
        {
            Node = node;
        }

        public Node Node { get; }
        public override string ToString()
            => Node.ToString();
    }

    public class BinaryExpressionNode : ExpressionNode
    {
        public BinaryExpressionNode(Node left, Node right)
        {
            Left = left;
            Right = right;
        }

        public Node Left { get; }
        public Node Right { get; }

        public override string ToString()
            => $"{Left.ToString()}..{Right.ToString()}";
    }

    public class DateRangeTests
    {
        [Theory]
        [InlineData("@now", "@now")]
        [InlineData("@now-1", "@now-1")]
        [InlineData("@now-2..@now-1", "@now-2..@now-1")]
        [InlineData("@now+2", "@now2")]
        [InlineData(">@now", ">@now")]
        [InlineData("2019-10-12", "2019-10-11T23:00:00.0000000Z")]
        [InlineData(">2019-10-12", ">2019-10-11T23:00:00.0000000Z")]
        [InlineData("2017-01-01T01:00:00+07:00", "2017-01-01T00:00:00.0000000Z")]
        public void DateParser(string text, string expected)
        {

            var operators = OneOf(Literals.Text(">"), Literals.Text(">="), Literals.Text("<"), Literals.Text("<="));

            var arithmetic = Terms.Integer(NumberOptions.AllowSign);
            var range = Literals.Text("..");

            var nowparser = Terms.Text("@now").And(ZeroOrOne(arithmetic))
                .Then<Node>(x => 
                {
                    if (x.Item2 != 0)
                    {
                        return new NowNode { Arithmetic = x.Item2 };
                    }

                    return new NowNode();
                });

            var dateParser = Terms.Pattern(x => Character.IsHexDigit(x) || x == '-' || x == ':' || x == '+')
                .Then<Node>((context, x) => 
                {
                    if (DateTimeOffset.TryParse(x.ToString(), out var dt))
                    {
                        return new DateNode {DateTime = dt.UtcDateTime};
                    }

                    throw new ParseException("Could not parse date", context.Scanner.Cursor.Position);
                });

            var valueParser = OneOf(nowparser, dateParser);

            var rangeParser = OneOf(nowparser, dateParser)
                .And(ZeroOrOne(range.SkipAnd(OneOf(nowparser, dateParser))))
                .Then<ExpressionNode>(x =>
                {
                    if (x.Item2 == null)
                    {
                        return new UnaryExpressionNode(x.Item1);
                    }

                    else
                    {
                        return new BinaryExpressionNode(x.Item1, x.Item2);
                    }
                });

            var parser = operators.And(nowparser.Or(dateParser))
                    .Then<ExpressionNode>(x => 
                    {
                        x.Item2.Operator = x.Item1;
                        return new UnaryExpressionNode(x.Item2);
                    })
                .Or(rangeParser);

            var result = parser.Parse(text);
            Assert.Equal(expected, result.ToString());

            //todo generate an expression, either single or binary
            // if more than two ignore them
        }
    }
}
