using YesSql.Filters.Nodes;
using YesSql.Filters.Services;
using Parlot.Fluent;
using static Parlot.Fluent.Parsers;

namespace YesSql.Filters.Builders
{
    /// <summary>
    /// Builds an operator parser that matches a single operand value, optionally quoted, without boolean composition.
    /// </summary>
    /// <typeparam name="T">The type the filter is applied to.</typeparam>
    /// <typeparam name="TTermOption">The type of the term options.</typeparam>
    public abstract class UnaryEngineBuilder<T, TTermOption> : OperatorEngineBuilder<T, TTermOption> where TTermOption : TermOption
    {
        private static readonly Parser<OperatorNode> _parser = OneOf(
                Terms.String(StringLiteralQuotes.Double).Then<OperatorNode>(static (node) => new UnaryNode(node.ToString(), OperateNodeQuotes.Double)),
                Terms.String(StringLiteralQuotes.Single).Then<OperatorNode>(static (node) => new UnaryNode(node.ToString(), OperateNodeQuotes.Single)),
                Terms.NonWhiteSpace().Then<OperatorNode>(static (node) => new UnaryNode(node.ToString(), OperateNodeQuotes.None))
            );

        /// <summary>
        /// The term options produced alongside the parser.
        /// </summary>
        protected TTermOption _termOption;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnaryEngineBuilder{T, TTermOption}"/> class.
        /// </summary>
        /// <param name="termOption">The term options to associate with the parser.</param>
        protected UnaryEngineBuilder(TTermOption termOption)
        {
            _termOption = termOption;
        }

        /// <summary>
        /// Configures the term to allow multiple occurrences instead of a single one.
        /// </summary>
        /// <returns>The current <see cref="UnaryEngineBuilder{T, TTermOption}"/> instance, to enable chaining.</returns>
        public UnaryEngineBuilder<T, TTermOption> AllowMultiple()
        {
            _termOption.Single = false;

            return this;
        }

        /// <summary>
        /// Configures the term to always run, even when it is not specified in the filter.
        /// </summary>
        /// <returns>The current <see cref="UnaryEngineBuilder{T, TTermOption}"/> instance, to enable chaining.</returns>
        public UnaryEngineBuilder<T, TTermOption> AlwaysRun()
        {
            _termOption.AlwaysRun = true;

            return this;
        }

        /// <summary>
        /// Builds the unary operator parser and its associated term options.
        /// </summary>
        /// <returns>A tuple containing the operator parser and the term options.</returns>
        public override (Parser<OperatorNode> Parser, TTermOption TermOption) Build()
            => (_parser, _termOption);
    }
}
