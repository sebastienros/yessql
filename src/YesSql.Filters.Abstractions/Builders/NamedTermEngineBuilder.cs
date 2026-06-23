using YesSql.Filters.Nodes;
using YesSql.Filters.Services;
using Parlot.Fluent;
using static Parlot.Fluent.Parsers;

namespace YesSql.Filters.Builders
{
    /// <summary>
    /// Builds a term parser that requires the term to be explicitly named using the <c>name:value</c> syntax.
    /// </summary>
    /// <typeparam name="T">The type the filter is applied to.</typeparam>
    /// <typeparam name="TTermOption">The type of the term options.</typeparam>
    public class NamedTermEngineBuilder<T, TTermOption> : TermEngineBuilder<T, TTermOption> where TTermOption : TermOption
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NamedTermEngineBuilder{T, TTermOption}"/> class.
        /// </summary>
        /// <param name="name">The name of the term.</param>
        public NamedTermEngineBuilder(string name) : base(name)
        {
        }

        /// <summary>
        /// Builds a parser that matches the term name followed by a colon and the term's operation, producing a <see cref="NamedTermNode"/>.
        /// </summary>
        /// <returns>A tuple containing the term parser and the term options.</returns>
        public override (Parser<TermNode> Parser, TTermOption TermOption) Build()
        {
            var op = _operatorParser.Build();

            var parser = Terms.Text(Name, caseInsensitive: true)
                .AndSkip(Literals.Char(':'))
                .And(op.Parser)
                    .Then<TermNode>(static x => new NamedTermNode(x.Item1, x.Item2));

            return (parser, op.TermOption);
        }                    
    }
}
