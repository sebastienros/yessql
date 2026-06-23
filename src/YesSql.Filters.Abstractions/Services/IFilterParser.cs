namespace YesSql.Filters.Services
{
    /// <summary>
    /// Defines a parser that converts a filter expression string into a strongly typed result.
    /// </summary>
    /// <typeparam name="TResult">The type produced by the parser.</typeparam>
    public interface IFilterParser<TResult>
    {
        /// <summary>
        /// Parses the specified filter expression.
        /// </summary>
        /// <param name="text">The filter expression to parse.</param>
        /// <returns>The parsed result.</returns>
        TResult Parse(string text);
    }
}
