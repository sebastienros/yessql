using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using YesSql.Indexes;

namespace YesSql
{
    public interface IQuery
    {
        /// <summary>
        /// Adds a filter on the document type
        /// </summary>
        /// <param name="filterType">If <c>false</c> the document type won't be filtered.</param>
        /// <typeparam name="T">The type of document to return</typeparam>
        IQuery<T> For<T>(bool filterType = true) where T : class;

        /// <summary>
        /// Defines what type of index should be returned
        /// </summary>
        /// <typeparam name="T">The type of index to return</typeparam>
        IQueryIndex<T> ForIndex<T>() where T : class, IIndex;

        /// <summary>
        /// Returns documents from any type
        /// </summary>
        IQuery<object> Any();
    }

    /// <summary>
    /// Represents a query over an entity
    /// </summary>
    /// <typeparam name="T">The type to return. It can be and index or an entity</typeparam>
    public interface IQuery<T> where T : class
    {
        /// <summary>
        /// Filters any predicates on newly joined indexes.
        /// </summary>
        IQuery<T> Any(params Func<IQuery<T>, IQuery<T>>[] predicates);

        /// <summary>
        /// Filters any predicates on newly joined indexes.
        /// </summary>
        ValueTask<IQuery<T>> AnyAsync(params Func<IQuery<T>, ValueTask<IQuery<T>>>[] predicates);

        /// <summary>
        /// Filters all predicates on newly joined indexes.
        /// </summary>
        IQuery<T> All(params Func<IQuery<T>, IQuery<T>>[] predicates);

        /// <summary>
        /// Filters all predicates on newly joined indexes.
        /// </summary>
        ValueTask<IQuery<T>> AllAsync(params Func<IQuery<T>, ValueTask<IQuery<T>>>[] predicates);

        /// <summary>
        /// Filters the documents with a record in the specified index.
        /// </summary>
        IQuery<T> With(Type indexType);

        /// <summary>
        /// Filters the documents with a record in the specified index.
        /// </summary>
        /// <typeparam name="TIndex">The index to filter on.</typeparam>
        IQuery<T, TIndex> With<TIndex>() where TIndex : class, IIndex;
        
        /// <summary>
        /// Filters the documents with a constraint on the specified index.
        /// </summary>
        /// <typeparam name="TIndex">The index to filter on.</typeparam>
        IQuery<T, TIndex> With<TIndex>(Expression<Func<TIndex, bool>> predicate) where TIndex : class, IIndex;
        
        /// <summary>
        /// Skips the specified number of document.
        /// </summary>
        /// <param name="count">The number of documents to skip.</param>
        IQuery<T> Skip(int count);

        /// <summary>
        /// Limits the results to the specified number of document.
        /// </summary>
        /// <param name="count">The number of documents to return.</param>
        IQuery<T> Take(int count);

        /// <summary>
        /// Executes the query and returns the first result matching the constraints.
        /// </summary>
        Task<T> FirstOrDefaultAsync();

        /// <summary>
        /// Executes the query and returns all documents matching the constraints.
        /// </summary>
        Task<IEnumerable<T>> ListAsync();

        /// <summary>
        /// Executes the query and returns all documents matching the constraints.
        /// </summary>
        IAsyncEnumerable<T> ToAsyncEnumerable();

        /// <summary>
        /// Executes a that returns the number of documents matching the constraints.
        /// </summary>
        Task<int> CountAsync();

        /// <summary>
        /// Returns the SQL alias currently used for the specified index type.
        /// </summary>
        string GetTypeAlias(Type t);
    }

    /// <summary>
    /// Represents a query over an index, which can be ordered.
    /// </summary>
    /// <typeparam name="T">The index's type to query over.</typeparam>
    public interface IQueryIndex<T> where T : IIndex
    {
        /// <summary>
        /// Joins the document table with an index.
        /// </summary>
        IQueryIndex<TIndex> With<TIndex>() where TIndex : class, IIndex;

        /// <summary>
        /// Joins the document table with an index, and filter it with a predicate.
        /// </summary>
        IQueryIndex<TIndex> With<TIndex>(Expression<Func<TIndex, bool>> predicate) where TIndex : class, IIndex;
        
        /// <summary>
        /// Adds a custom Where clause to the query.
        /// </summary>
        IQueryIndex<T> Where(string sql);

        /// <summary>
        /// Adds a custom Where clause to the query using a specific dialect. 
        /// </summary>
        IQueryIndex<T> Where(Func<ISqlDialect, string> sql);

        /// <summary>
        /// Adds a named parameter to the query.
        /// </summary>
        IQueryIndex<T> WithParameter(string name, object value);

        /// <summary>
        /// Adds a named parameter to the query.
        /// </summary>
        IQueryIndex<T> Where(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Sets an OrderBy clause using a custom lambda expression.
        /// </summary>
        IQueryIndex<T> OrderBy(Expression<Func<T, object>> keySelector);

        /// <summary>
        /// Sets a descending OrderBy clause using a custom lambda expression.
        /// </summary>
        IQueryIndex<T> OrderByDescending(Expression<Func<T, object>> keySelector);

        /// <summary>
        /// Adds an OrderBy clause using a custom lambda expression.
        /// </summary>
        IQueryIndex<T> ThenBy(Expression<Func<T, object>> keySelector);
        
        /// <summary>
        /// Adds a descending OrderBy clause using a custom lambda expression.
        /// </summary>
        IQueryIndex<T> ThenByDescending(Expression<Func<T, object>> keySelector);

        /// <summary>
        /// Skips some results.
        /// </summary>
        IQueryIndex<T> Skip(int count);

        /// <summary>
        /// Limits the number of results.
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        IQueryIndex<T> Take(int count);

        /// <summary>
        /// Returns the first result only, if it exists.
        /// </summary>
        Task<T> FirstOrDefaultAsync();

        /// <summary>
        /// Executes the query.
        /// </summary>
        Task<IEnumerable<T>> ListAsync();

        /// <summary>
        /// Executes the query for asynchronous iteration.
        /// </summary>
        /// <returns></returns>
        IAsyncEnumerable<T> ToAsyncEnumerable();

        /// <summary>
        /// Returns the number of results only.
        /// </summary>
        Task<int> CountAsync();
    }

    /// <summary>
    /// Represents a query over an index that targets a specific entity.
    /// </summary>
    /// <typeparam name="T">The entity's type to return.</typeparam>
    /// <typeparam name="TIndex">The index's type to query over.</typeparam>
    public interface IQuery<T, TIndex> : IQuery<T>
        where T : class
        where TIndex : IIndex
    {
        /// <summary>
        /// Adds a custom Where clause to the query.
        /// </summary>
        IQuery<T, TIndex> Where(string sql);

        /// <summary>
        /// Adds a custom Where clause to the query using a specific dialect. 
        /// </summary>
        IQuery<T, TIndex> Where(Func<ISqlDialect, string> sql);
        
        /// <summary>
        /// Adds a named parameter to the query.
        /// </summary>
        IQuery<T, TIndex> WithParameter(string name, object value);
        
        /// <summary>
        /// Adds a named parameter to the query.
        /// </summary>
        IQuery<T, TIndex> Where(Expression<Func<TIndex, bool>> predicate);
        
        /// <summary>
        /// Sets an OrderBy clause using a custom lambda expression.
        /// </summary>
        IQuery<T, TIndex> OrderBy(Expression<Func<TIndex, object>> keySelector);
        
        /// <summary>
        /// Sets an OrderBy clause using a custom SQL statement.
        /// </summary>
        IQuery<T, TIndex> OrderBy(string sql);
        
        IQuery<T, TIndex> OrderByDescending(Expression<Func<TIndex, object>> keySelector);
        
        /// <summary>
        /// Sets a descending OrderBy clause using a custom SQL statement.
        /// </summary>
        IQuery<T, TIndex> OrderByDescending(string sql);
        
        /// <summary>
        /// Sets a random OrderBy clause.
        /// </summary>
        IQuery<T, TIndex> OrderByRandom();

        /// <summary>
        /// Adds an OrderBy clause using a custom lambda expression.
        /// </summary>
        IQuery<T, TIndex> ThenBy(Expression<Func<TIndex, object>> keySelector);

        /// <summary>
        /// Adds an OrderBy clause using a custom SQL statement.
        /// </summary>
        IQuery<T, TIndex> ThenBy(string sql);

        /// <summary>
        /// Adds a descending OrderBy clause using a custom lambda expression.
        /// </summary>
        IQuery<T, TIndex> ThenByDescending(Expression<Func<TIndex, object>> keySelector);

        /// <summary>
        /// Adds a descending OrderBy clause using a custom SQL statement.
        /// </summary>
        IQuery<T, TIndex> ThenByDescending(string sql);

        /// <summary>
        /// Adds a random OrderBy clause.
        /// </summary>
        IQuery<T, TIndex> ThenByRandom();
    }
}
