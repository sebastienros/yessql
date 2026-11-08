using System;

namespace YesSql.Provider
{
    /// <summary>
    /// Implemented by an <see cref="ISqlDialect"/> whose database accepts arrays as parameter values, so that a list of
    /// values (e.g. <c>IsIn</c>) is passed as a single parameter.
    /// </summary>
    /// <remarks>
    /// With one parameter per value, the SQL text changes with the number of values, and each length is a different
    /// statement for prepared statement and query plan caches.
    /// </remarks>
    public interface IArrayParameterDialect
    {
        /// <summary>
        /// Gets whether an array of values of this type can be passed as a parameter.
        /// </summary>
        bool SupportsArrayParameter(Type elementType);

        /// <summary>
        /// Returns the SQL comparing an expression with the values of an array parameter, e.g. <c> = any(@p0)</c>.
        /// </summary>
        string InArrayOperator(string parameterName);

        /// <summary>
        /// Returns the SQL checking that an expression is not one of the values of an array parameter, e.g. <c> &lt;&gt; all(@p0)</c>.
        /// </summary>
        string NotInArrayOperator(string parameterName);
    }
}
