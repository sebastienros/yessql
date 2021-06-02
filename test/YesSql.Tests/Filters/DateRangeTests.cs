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
using System.Linq.Expressions;

namespace YesSql.Tests.Filters
{
    public readonly struct BuildExpressionContext
    {
        public BuildExpressionContext(DateTime utcNow, ParameterExpression parameter, MemberExpression member, Type type)
        {
            UtcNow = utcNow;
            Parameter = parameter;
            Member = member;
            Type = type;
        }

        public DateTime UtcNow { get; }
        public ParameterExpression Parameter { get; }
        public MemberExpression Member { get; }
        public Type Type { get; }
    }

    public abstract class Node
    {
        public abstract Expression BuildExpression(BuildExpressionContext context);
    }

    public abstract class OperatorNode : Node
    {
        public string Operator { get; set; }

        public Expression BuildOperation(BuildExpressionContext context, ConstantExpression constant)
        {
            if (String.IsNullOrEmpty(Operator))
            {
                return constant;
            }

            return Operator switch
            {
                ">" => Expression.GreaterThan(context.Member, constant),
                ">=" => Expression.GreaterThanOrEqual(context.Member, constant),
                "<" => Expression.LessThan(context.Member, constant),
                "<=" => Expression.LessThanOrEqual(context.Member, constant),
                _ => null
            };
        }
    }

    public class DateNode : OperatorNode
    {
        public DateNode(DateTime dateTime)
        {
            DateTime = dateTime;
        }

        public DateTime DateTime { get; }

        public override Expression BuildExpression(BuildExpressionContext context)
            => BuildOperation(context, Expression.Constant(DateTime, typeof(DateTime)));

        public override string ToString()
            => $"{(String.IsNullOrEmpty(Operator) ? String.Empty : Operator)}{DateTime.ToString("o")}";
    }

    public class NowNode : OperatorNode
    {
        public NowNode()
        {
        }
        public NowNode(long arithmetic)
        {
            Arithmetic = arithmetic;
        }

        public long? Arithmetic { get; }

        public override Expression BuildExpression(BuildExpressionContext context)
            => BuildOperation(context, Expression.Constant(context.UtcNow.AddDays(Arithmetic.GetValueOrDefault()), typeof(DateTime)));

        public override string ToString()
            => $"{(String.IsNullOrEmpty(Operator) ? String.Empty : Operator)}@now{(Arithmetic.HasValue ? Arithmetic.Value.ToString() : String.Empty)}";
    }

    public abstract class ExpressionNode : Node
    { }

    public class UnaryExpressionNode : ExpressionNode
    {
        public UnaryExpressionNode(OperatorNode node)
        {
            Node = node;
        }

        public override Expression BuildExpression(BuildExpressionContext context)
            => Expression.Lambda(context.Type, Node.BuildExpression(context), context.Parameter);

        public OperatorNode Node { get; }
        public override string ToString()
            => Node.ToString();
    }

    public class BinaryExpressionNode : ExpressionNode
    {
        public BinaryExpressionNode(OperatorNode left, OperatorNode right)
        {
            Left = left;
            Right = right;
        }

        public OperatorNode Left { get; }
        public OperatorNode Right { get; }

        public override Expression BuildExpression(BuildExpressionContext context)
        {
            var left = Expression.GreaterThanOrEqual(context.Member, Left.BuildExpression(context));
            var right = Expression.LessThanOrEqual(context.Member, Right.BuildExpression(context));

            return Expression.Lambda(context.Type, Expression.AndAlso(left, right), context.Parameter);
        }

        public override string ToString()
            => $"{Left.ToString()}..{Right.ToString()}";
    }

    public static class DateParser
    {
        public static Parser<ExpressionNode> Parser;

        static DateParser()
        {
            var operators = OneOf(Literals.Text(">"), Literals.Text(">="), Literals.Text("<"), Literals.Text("<="));

            var arithmetic = Terms.Integer(NumberOptions.AllowSign);
            var range = Literals.Text("..");

            var nowparser = Terms.Text("@now").And(ZeroOrOne(arithmetic))
                .Then<OperatorNode>(x =>
                {
                    if (x.Item2 != 0)
                    {
                        return new NowNode(x.Item2);
                    }

                    return new NowNode();
                });

            var dateParser = Terms.Pattern(x => Character.IsHexDigit(x) || x == '-' || x == ':' || x == '+')
                .Then<OperatorNode>((context, x) =>
                {
                    if (DateTimeOffset.TryParse(x.ToString(), out var dt))
                    {
                        return new DateNode(dt.UtcDateTime);
                    }

                    throw new ParseException("Could not parse date", context.Scanner.Cursor.Position);
                });

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

            Parser = operators.And(nowparser.Or(dateParser))
                    .Then<ExpressionNode>(x =>
                    {
                        x.Item2.Operator = x.Item1;
                        return new UnaryExpressionNode(x.Item2);
                    })
                .Or(rangeParser).Compile();
        }
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
        [InlineData("2019-10-12..2019-10-12", "2019-10-11T23:00:00.0000000Z..2019-10-11T23:00:00.0000000Z")]
        [InlineData(">2019-10-12", ">2019-10-11T23:00:00.0000000Z")]
        [InlineData("2017-01-01T01:00:00+07:00..2017-02-01T01:00:00+07:00", "2017-01-01T00:00:00.0000000Z..2017-02-01T01:00:00+07:00")]
        public void DateParserTests(string text, string expected)
        {
            var result = DateParser.Parser.Parse(text);
            Assert.Equal(expected, result.ToString());
        }
    }
}
