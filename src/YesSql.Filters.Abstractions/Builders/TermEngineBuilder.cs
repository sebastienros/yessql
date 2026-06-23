using YesSql.Filters.Nodes;
using YesSql.Filters.Services;
using Parlot.Fluent;

namespace YesSql.Filters.Builders
{
    /// <summary>
    /// Represents the base class for builders that produce a parser for a named term in a filter, delegating the term's operations to an <see cref="OperatorEngineBuilder{T, TTermOption}"/>.
    /// </summary>
    /// <typeparam name="T">The type the filter is applied to.</typeparam>
    /// <typeparam name="TTermOption">The type of the term options.</typeparam>
    public abstract class TermEngineBuilder<T, TTermOption>  where TTermOption : TermOption
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TermEngineBuilder{T, TTermOption}"/> class.
        /// </summary>
        /// <param name="name">The name of the term.</param>
        protected TermEngineBuilder(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Gets the name of the term.
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// Gets a value indicating whether only a single occurrence of the term is allowed.
        /// </summary>
        public bool Single { get; }

        /// <summary>
        /// The builder that produces the parser for the term's operations.
        /// </summary>
        protected OperatorEngineBuilder<T, TTermOption> _operatorParser;

        /// <summary>
        /// Sets the operator builder used to parse the term's operations.
        /// </summary>
        /// <param name="operatorParser">The operator builder to use.</param>
        /// <returns>The current <see cref="TermEngineBuilder{T, TTermOption}"/> instance, to enable chaining.</returns>
        public TermEngineBuilder<T, TTermOption> SetOperator(OperatorEngineBuilder<T, TTermOption> operatorParser)
        {
            _operatorParser = operatorParser;

            return this;
        }

        /// <summary>
        /// Builds the term parser and its associated term options.
        /// </summary>
        /// <returns>A tuple containing the term parser and the term options.</returns>
        public abstract (Parser<TermNode> Parser, TTermOption TermOption) Build();
    }
}
