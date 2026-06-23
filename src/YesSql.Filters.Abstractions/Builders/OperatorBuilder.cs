using YesSql.Filters.Nodes;
using YesSql.Filters.Services;
using Parlot.Fluent;

namespace YesSql.Filters.Builders
{
    /// <summary>
    /// Represents the base class for builders that produce a parser for the operations applied within a term, together with its term options.
    /// </summary>
    /// <typeparam name="T">The type the filter is applied to.</typeparam>
    /// <typeparam name="TTermOption">The type of the term options.</typeparam>
    public abstract class OperatorEngineBuilder<T, TTermOption> where TTermOption : TermOption
    {
        /// <summary>
        /// Builds the operator parser and its associated term options.
        /// </summary>
        /// <returns>A tuple containing the operator parser and the term options.</returns>
        public abstract (Parser<OperatorNode> Parser, TTermOption TermOption) Build();
    }
}
