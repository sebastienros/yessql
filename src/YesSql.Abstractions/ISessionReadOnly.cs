using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using YesSql.Indexes;

namespace YesSql
{
    /// <summary>
    /// Represents a connection to the document store.
    /// </summary>
    public interface ISessionReadOnly : IDisposable
    {
        /// <summary>
        /// Loads objects by id.
        /// </summary>
        /// <returns>A collection of objects in the same order they were defined.</returns>
        Task<IEnumerable<T>> GetAsync<T>(int[] ids, string collection = null) where T : class;

        /// <summary>
        /// Creates a new <see cref="IQuery"/> object.
        /// </summary>
        IQuery Query(string collection = null);

        /// <summary>
        /// Executes a compiled query.
        /// </summary>
        /// <remarks>
        /// A compiled query is an instance of a class implementing <see cref="ICompiledQuery{T}" />.
        /// Compiled queries allow YesSql to cache the SQL statement that would be otherwise generated
        /// on each invocation of the LINQ query. 
        /// </remarks>
        IQuery<T> ExecuteQuery<T>(ICompiledQuery<T> compiledQuery, string collection = null) where T : class;


        /// <summary>
        /// Gets the <see cref="Store" /> instance that created this session. 
        /// </summary>
        IStore Store { get; }
    }
    /// <summary>
    /// Represents a connection to the document store.
    /// </summary>
    

    public static class SessionReadOnlyExtensions
    {
        /// <summary>
        /// Loads an object by its id.
        /// </summary>
        /// <returns>The object or <c>null</c>.</returns>
        public async static Task<T> GetAsync<T>(this ISessionReadOnly sessionReadOnly, int id, string collection = null) where T : class
        {
            return (await sessionReadOnly.GetAsync<T>(new[] { id }, collection)).FirstOrDefault();
        }
    }
}
