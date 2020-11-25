namespace YesSql
{
    /// <summary>
    /// A class implementing this interface can be used to represent custom function calls in a dialect.
    /// </summary>
    public interface ISqlFunction
    {

        /// <summary>
        /// Renders the function. 
        /// </summary>
        string Render(string[] arguments);
    }
}
