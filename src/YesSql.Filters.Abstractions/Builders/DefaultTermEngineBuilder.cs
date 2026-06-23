using YesSql.Filters.Nodes;
using YesSql.Filters.Services;
using Parlot.Fluent;
using static Parlot.Fluent.Parsers;

namespace YesSql.Filters.Builders
{
    /// <summary>
    /// Builds a term parser that accepts the term either explicitly named using the <c>name:value</c> syntax or unnamed, in which case it is treated as the default term.
    /// </summary>
    /// <typeparam name="T">The type the filter is applied to.</typeparam>
    /// <typeparam name="TTermOption">The type of the term options.</typeparam>
    public class DefaultTermEngineBuilder<T, TTermOption> : TermEngineBuilder<T, TTermOption> where T : class where TTermOption : TermOption
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultTermEngineBuilder{T, TTermOption}"/> class.
        /// </summary>
        /// <param name="name">The name of the term.</param>
        public DefaultTermEngineBuilder(string name) : base(name)
        {
        }

        /// <summary>
        /// Builds a parser that produces a <see cref="NamedTermNode"/> when the term is named, or a <see cref="DefaultTermNode"/> when it is not.
        /// </summary>
        /// <returns>A tuple containing the term parser and the term options.</returns>
        public override (Parser<TermNode> Parser, TTermOption TermOption) Build()
        {
            var op = _operatorParser.Build();

            var termParser = Terms.Text(Name, caseInsensitive: true)
                .AndSkip(Literals.Char(':'))
                .And(op.Parser)
                    .Then<TermNode>(static x => new NamedTermNode(x.Item1, x.Item2));

            var defaultParser = op.Parser.Then<TermNode>(x => new DefaultTermNode(Name, x));

            var parser = termParser.Or(defaultParser);

            return (parser, op.TermOption);
        }
    }
}
