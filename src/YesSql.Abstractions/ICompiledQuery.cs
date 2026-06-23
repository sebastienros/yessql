using System;
using System.Linq.Expressions;

namespace YesSql
{
    /// <summary>
    /// And implementation of this interface can be reused 
    /// to prevent multiple evaluations of the same expression
    /// tree.
    /// </summary>
    /// <typeparam name="T">The type of object the query returns.</typeparam>
    public interface ICompiledQuery<T> where T : class
    {
        /// <summary>
        /// Gets the expression tree representing the compiled query.
        /// </summary>
        /// <returns>An expression that transforms an <see cref="IQuery{T}"/> into the desired query.</returns>
        Expression<Func<IQuery<T>, IQuery<T>>> Query();
    }
}
