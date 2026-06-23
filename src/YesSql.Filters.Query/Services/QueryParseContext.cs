using System.Collections.Generic;
using Parlot;
using Parlot.Fluent;

namespace YesSql.Filters.Query.Services
{
    /// <summary>
    /// Represents the parse context used while parsing a query filter.
    /// </summary>
    /// <typeparam name="T">The type of the queried document.</typeparam>
    public class QueryParseContext<T> : ParseContext where T : class
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueryParseContext{T}"/> class.
        /// </summary>
        /// <param name="termOptions">The available term options.</param>
        /// <param name="scanner">The scanner used to read the input text.</param>
        /// <param name="useNewLines">Whether new lines are significant while parsing.</param>
        public QueryParseContext(IReadOnlyDictionary<string, QueryTermOption<T>> termOptions, Scanner scanner, bool useNewLines = false) : base(scanner, useNewLines)
        {
            TermOptions = termOptions;
        }

        /// <summary>
        /// Gets the available term options.
        /// </summary>
        public IReadOnlyDictionary<string, QueryTermOption<T>> TermOptions { get; }
    }
}
