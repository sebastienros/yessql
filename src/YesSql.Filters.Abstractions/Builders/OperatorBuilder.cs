using YesSql.Filters.Nodes;
using YesSql.Filters.Services;
using Parlot.Fluent;

namespace YesSql.Filters.Builders
{
    public abstract class OperatorEngineBuilder<T, TTermOption> where TTermOption : TermOption
    {
        public abstract (Parser<OperatorNode> Parser, TTermOption TermOption) Build();
    }
}
