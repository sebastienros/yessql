
using YesSql.Filters.Abstractions.Nodes;
using YesSql.Filters.Abstractions.Services;
using Parlot.Fluent;
using static Parlot.Fluent.Parsers;

namespace YesSql.Filters.Abstractions.Builders
{
    public abstract class BooleanEngineBuilder<T, TTermOption> : OperatorEngineBuilder<T, TTermOption> where TTermOption : TermOption
    {
        private static Parser<OperatorNode> _parser;
        protected TTermOption _termOption;

        static BooleanEngineBuilder()
        {
            var OperatorNode = Deferred<OperatorNode>();

            var AndOperator = Terms.Text("AND")
                .Or(
                    Terms.Text("&&")
                );

            var NotOperator = Terms.Text("NOT")
                .Or(
                    Terms.Text("!")
                );

            var OrTextOperators = Terms.Text("OR")
                .Or(
                    Terms.Text("||")
                );

            // Operators that need to be NOT next when the default OR ' ' operator is found.
            var NotOrOperators = OneOf(AndOperator, NotOperator, OrTextOperators);

            // Default operator.
            var OrOperator = Literals.WhiteSpace()
                .Then<string>(static x => " ") // Normalize whitespace.
                .AndSkip(Not(NotOrOperators))
                .Or(
                    OrTextOperators
                );

            var GroupNode = Between(Terms.Char('('), OperatorNode, Terms.Char(')'))
                .Then<OperatorNode>(static node => new GroupNode(node));

            var Breaks = OneOf(Terms.Pattern(static x => x == ':' || x == '(' || x == ')'), Literals.WhiteSpace());

            // A term name is never enclosed in strings.
            var DoubleQuotedNode = Terms.String(StringLiteralQuotes.Double)
                .Then<OperatorNode>(static (node) => new UnaryNode(node.ToString(), OperateNodeQuotes.Double));

            var SingleQuotedNode = Terms.String(StringLiteralQuotes.Single)
                .Then<OperatorNode>(static (node) => new UnaryNode(node.ToString(), OperateNodeQuotes.Single));

            // This must be aborted when it is consuming the next term.
            var UnquotedNode = SkipWhiteSpace(AnyCharBefore(Breaks).AndSkip(Not(Literals.Char(':'))))
                .Then<OperatorNode>(static (node) => new UnaryNode(node.ToString(), OperateNodeQuotes.None));

            var SingleNode = OneOf(DoubleQuotedNode, SingleQuotedNode, UnquotedNode);

            var Primary = SingleNode.Or(GroupNode);

            var UnaryNode = NotOperator.And(Primary)
                .Then<OperatorNode>(static (node) =>
                {
                    // mutate with the neg query.
                    var unaryNode = node.Item2 as UnaryNode;

                    return new NotUnaryNode(node.Item1, new UnaryNode(unaryNode.Value, unaryNode.Quotes, false));
                })
                .Or(Primary);

            var AndNode = UnaryNode.And(ZeroOrMany(AndOperator.And(UnaryNode)))
                .Then<OperatorNode>(static node =>
                {
                    // unary
                    var result = node.Item1;

                    foreach (var op in node.Item2)
                    {
                        result = new AndNode(result, op.Item2, op.Item1);
                    }

                    return result;
                });

            OperatorNode.Parser = AndNode.And(ZeroOrMany(NotOperator.Or(OrOperator).And(AndNode)))
               .Then<OperatorNode>(static (node) =>
               {
                   static NotNode CreateNotNode(OperatorNode result, (string, OperatorNode) op)
                       => new NotNode(result, new UnaryNode(((UnaryNode)op.Item2).Value, ((UnaryNode)op.Item2).Quotes, false), op.Item1);

                   static OrNode CreateOrNode(OperatorNode result, (string, OperatorNode) op)
                       => new OrNode(result, op.Item2, op.Item1);

                   // unary
                   var result = node.Item1;

                   foreach (var op in node.Item2)
                   {
                       result = op.Item1 switch
                       {
                           "NOT" => CreateNotNode(result, op),
                           "!" => CreateNotNode(result, op),
                           "OR" => CreateOrNode(result, op),
                           "||" => CreateOrNode(result, op),
                           " " => CreateOrNode(result, op),
                           _ => null
                       };
                   }

                   return result;
               });

            _parser = OperatorNode;
        }

        public override (Parser<OperatorNode> Parser, TTermOption TermOption) Build()
            => (_parser, _termOption);
    }
}
