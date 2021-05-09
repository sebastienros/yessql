using YesSql.Filters.Abstractions.Nodes;
using YesSql.Filters.Abstractions.Services;
using Parlot.Fluent;

namespace YesSql.Filters.Abstractions.Builders
{
    public abstract class OperatorEngineBuilder<T, TTermOption> where TTermOption : TermOption
    {
        public abstract (Parser<OperatorNode> Parser, TTermOption TermOption) Build();
    }
}
