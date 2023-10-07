using YesSql.Filters.Nodes;
using YesSql.Filters.Services;
using Parlot.Fluent;
using static Parlot.Fluent.Parsers;

namespace YesSql.Filters.Builders
{
    public class NamedTermEngineBuilder<T, TTermOption> : TermEngineBuilder<T, TTermOption> where TTermOption : TermOption
    {
        public NamedTermEngineBuilder(string name) : base(name)
        {
        }

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
