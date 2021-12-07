using Parlot.Fluent;
using System;
using System.Globalization;
using System.Linq.Expressions;
using Xunit;
using static Parlot.Fluent.Parsers;

namespace YesSql.Tests.Filters.Numeric
{
    public readonly struct BuildExpressionContext
    {
        public BuildExpressionContext(ParameterExpression parameter, MemberExpression member, Type type)
        {
            Parameter = parameter;
            Member = member;
            Type = type;
        }

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

    public class DecimalNode : OperatorNode
    {
        public DecimalNode(decimal value)
        {
            Value = value;
        }

        public decimal Value { get; }

        public override Expression BuildExpression(BuildExpressionContext context)
            => BuildOperation(context, Expression.Constant(Value, typeof(decimal)));

        public override string ToString()
            => $"{(String.IsNullOrEmpty(Operator) ? String.Empty : Operator)}{Value}";
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
            => $"{Left}..{Right}";
    }

    public static class NumericParser
    {
        public static Parser<ExpressionNode> Parser;

        static NumericParser()
        {
            var operators = OneOf(Literals.Text(">="), Literals.Text(">"), Literals.Text("<="), Literals.Text("<"));

            var range = Literals.Text("..");

            var numericParser = new CustomDecimalLiteral(NumberOptions.AllowSign)
                .Then<OperatorNode>(x =>
                {
                    return new DecimalNode(x);
                });

            var rangeParser = numericParser
                .And(ZeroOrOne(range.SkipAnd(numericParser)))
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

            Parser = operators
                        .And(numericParser)
                            .Then<ExpressionNode>(x =>
                            {
                                x.Item2.Operator = x.Item1;
                                return new UnaryExpressionNode(x.Item2);
                            })
                        .Or(rangeParser);
            // Can't compile DecimalLiteral doesn't support it.
        }
    }

    public class NumericRangeTests
    {
        [Theory]
        [InlineData("1", "1")]
        [InlineData(">1", ">1")]
        [InlineData(">1.1", ">1.1")]
        [InlineData("<1", "<1")]
        [InlineData(">=1", ">=1")]
        [InlineData("<=1", "<=1")]
        [InlineData("1..2", "1..2")]
        [InlineData("1.1..2", "1.1..2")]
        public void NumericParserTests(string text, string expected)
        {
            var result = NumericParser.Parser.Parse(text);
            Assert.Equal(expected, result.ToString());
        }

        [Theory]
        [InlineData(">1,1", ">1,1")]
        // It will parse 1.1 but be reformatted as 1,1
        // The decimal is still 1.1 (i.e. culture invariant, so sql should parse ok
        [InlineData(">1.1", ">1,1")]
        [InlineData("1,1..2", "1,1..2")]
        [InlineData("1.1..2", "1,1..2")]
        public void NumericParserEuropeCultureTests(string text, string expected)
        {
            var originalCulture = CultureInfo.CurrentCulture;
            var greek = CultureInfo.GetCultureInfo("el-GR");

            CultureInfo.CurrentCulture = greek;
            var result = NumericParser.Parser.Parse(text);
            Assert.Equal(expected, result.ToString());
            CultureInfo.CurrentCulture = originalCulture;

        }
    }
}
