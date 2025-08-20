using YesSql.Filters.Abstractions.Nodes;
using YesSql.Filters.Abstractions.Services;
using Parlot.Fluent;
using static Parlot.Fluent.Parsers;

namespace YesSql.Filters.Abstractions.Builders
{
    public abstract class UnaryEngineBuilder<T, TTermOption> : OperatorEngineBuilder<T, TTermOption> where TTermOption : TermOption
    {
        private static readonly Parser<OperatorNode> _parser = OneOf(
                Terms.String(StringLiteralQuotes.Double).Then<OperatorNode>(static (node) => new UnaryNode(node.ToString(), OperateNodeQuotes.Double)),
                Terms.String(StringLiteralQuotes.Single).Then<OperatorNode>(static (node) => new UnaryNode(node.ToString(), OperateNodeQuotes.Single)),
                Terms.NonWhiteSpace().Then<OperatorNode>(static (node) => new UnaryNode(node.ToString(), OperateNodeQuotes.None))
            );

        protected TTermOption _termOption;

        protected UnaryEngineBuilder(TTermOption termOption)
        {
            _termOption = termOption;
        }

        public UnaryEngineBuilder<T, TTermOption> AllowMultiple()
        {
            _termOption.Single = false;

            return this;
        }

        public UnaryEngineBuilder<T, TTermOption> AlwaysRun()
        {
            _termOption.AlwaysRun = true;

            return this;
        }

        public override (Parser<OperatorNode> Parser, TTermOption TermOption) Build()
            => (_parser, _termOption);
    }
}
