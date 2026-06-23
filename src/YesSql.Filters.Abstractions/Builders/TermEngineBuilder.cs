using YesSql.Filters.Nodes;
using YesSql.Filters.Services;
using Parlot.Fluent;

namespace YesSql.Filters.Builders
{
    public abstract class TermEngineBuilder<T, TTermOption>  where TTermOption : TermOption
    {
        protected TermEngineBuilder(string name)
        {
            Name = name;
        }

        public string Name { get; }
        public bool Single { get; }

        protected OperatorEngineBuilder<T, TTermOption> _operatorParser;

        public TermEngineBuilder<T, TTermOption> SetOperator(OperatorEngineBuilder<T, TTermOption> operatorParser)
        {
            _operatorParser = operatorParser;

            return this;
        }

        public abstract (Parser<TermNode> Parser, TTermOption TermOption) Build();
    }
}
