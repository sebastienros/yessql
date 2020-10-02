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
        /// Filters all predicates on newly joined indexes.
        /// </summary>
        IQuery<T> All(params Func<IQuery<T>, IQuery<T>>[] predicates);

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
    }

    /// <summary>
    /// Represents a query over an index, which can be ordered.
    /// </summary>
    /// <typeparam name="T">The index's type to query over.</typeparam>
    public interface IQueryIndex<T> where T : IIndex
    {
        IQueryIndex<TIndex> With<TIndex>() where TIndex : class, IIndex;
        IQueryIndex<TIndex> With<TIndex>(Expression<Func<TIndex, bool>> predicate) where TIndex : class, IIndex;
        IQueryIndex<T> Where(string sql);
        IQueryIndex<T> Where(Func<ISqlDialect, string> sql);
        IQueryIndex<T> WithParameter(string name, object value);
        IQueryIndex<T> Where(Expression<Func<T, bool>> predicate);
        IQueryIndex<T> OrderBy(Expression<Func<T, object>> keySelector);
        IQueryIndex<T> OrderByDescending(Expression<Func<T, object>> keySelector);
        IQueryIndex<T> ThenBy(Expression<Func<T, object>> keySelector);
        IQueryIndex<T> ThenByDescending(Expression<Func<T, object>> keySelector);
        IQueryIndex<T> Skip(int count);
        IQueryIndex<T> Take(int count);
        Task<T> FirstOrDefaultAsync();
        Task<IEnumerable<T>> ListAsync();
        IAsyncEnumerable<T> ToAsyncEnumerable();
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
        //IQuery<T, TIndex> Any(params Func<IQuery<T, TIndex>, IQuery<T, TIndex>>[] predicates);
        //IQuery<T, TIndex> All(params Func<IQuery<T, TIndex>, IQuery<T, TIndex>>[] predicates);

        IQuery<T, TIndex> Where(string sql);
        IQuery<T, TIndex> Where(Func<ISqlDialect, string> sql);
        IQuery<T, TIndex> WithParameter(string name, object value);
        IQuery<T, TIndex> Where(Expression<Func<TIndex, bool>> predicate);
        IQuery<T, TIndex> OrderBy(Expression<Func<TIndex, object>> keySelector);
        IQuery<T, TIndex> OrderBy(string sql);
        IQuery<T, TIndex> OrderByDescending(Expression<Func<TIndex, object>> keySelector);
        IQuery<T, TIndex> OrderByDescending(string sql);
        IQuery<T, TIndex> OrderByRandom();
        IQuery<T, TIndex> ThenBy(Expression<Func<TIndex, object>> keySelector);
        IQuery<T, TIndex> ThenBy(string sql);
        IQuery<T, TIndex> ThenByDescending(Expression<Func<TIndex, object>> keySelector);
        IQuery<T, TIndex> ThenByDescending(string sql);
        IQuery<T, TIndex> ThenByRandom();
    }
}
