using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using YesSql.Indexes;

namespace YesSql
{
    /// <summary>
    /// Represents an untyped query used to select the document or index type to return.
    /// </summary>
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
        Task<T> FirstOrDefaultAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes the query and returns all documents matching the constraints.
        /// </summary>
        /// <remarks>
        /// The results are buffered in memory. Use <see cref="ToAsyncEnumerable(CancellationToken)"/> to stream them instead.
        /// </remarks>
        Task<IReadOnlyList<T>> ListAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes the query and returns all documents matching the constraints.
        /// </summary>
        IAsyncEnumerable<T> ToAsyncEnumerable(CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes a that returns the number of documents matching the constraints.
        /// </summary>
        Task<int> CountAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns the SQL alias currently used for the specified index type.
        /// </summary>
        string GetTypeAlias(Type t);

        /// <summary>
        /// Skips removing the documents returned several times when an index has multiple records for a document, for a query
        /// that is known to match a single record per document: the results aren't grouped by document, and
        /// <see cref="CountAsync"/> uses <c>count(*)</c> instead of a distinct count.
        /// </summary>
        /// <remarks>
        /// An index that always has a single record per document should declare it with
        /// <see cref="IMapFor{T, TIndex}.SingleRecordPerDocument"/> instead, which fails when a document maps several records.
        /// This method is for what a declaration can't express, like a filter that only keeps one of the records of each document.
        /// Nothing verifies it, so a document with several matches is returned and counted several times.
        /// It still applies with <see cref="NoIndexOptimizations"/>, as it isn't declared by an index.
        /// </remarks>
        IQuery<T> SkipDeduplication()
        {
            // Implementations written before this method only implement NoDuplicates().
#pragma warning disable CS0618 // Type or member is obsolete
            return NoDuplicates();
#pragma warning restore CS0618 // Type or member is obsolete
        }

        /// <summary>
        /// Skips removing the documents returned several times. Use <see cref="SkipDeduplication"/> instead.
        /// </summary>
        [Obsolete("Use SkipDeduplication() instead, as this method doesn't remove duplicates but skips removing them.")]
        IQuery<T> NoDuplicates();

        /// <summary>
        /// Runs the query as if its indexes declared nothing, ignoring
        /// <see cref="Indexes.IMapFor{T, TIndex}.ExclusiveToDocumentType"/> and <see cref="IMapFor{T, TIndex}.SingleRecordPerDocument"/>:
        /// the results are grouped by document, and the count joins the document table and is distinct, unless the query uses
        /// <see cref="SkipDeduplication"/>. This is for a query these optimizations make slower, or whose indexes have rows that don't
        /// match their declarations.
        /// </summary>
        IQuery<T> NoIndexOptimizations() => this;
    }

    /// <summary>
    /// Represents a query over an index, which can be ordered.
    /// </summary>
    /// <typeparam name="T">The index's type to query over.</typeparam>
    public interface IQueryIndex<T> where T : IIndex
    {
        /// <summary>
        /// Keeps the records matching any of the predicates. A predicate filters the current records with <c>Where()</c>,
        /// or joins another index on their documents with <c>With()</c>, like <c>q =&gt; q.With&lt;PersonByAge&gt;(x =&gt; x.Age &lt; 18)</c>.
        /// </summary>
        /// <remarks>
        /// Each predicate joins its indexes with new aliases, so a record is only returned when its document has a record in each of them,
        /// and it repeats for each combination of their records, like with <see cref="With{TIndex}(Expression{Func{TIndex, bool}})"/>.
        /// The query still returns the records of <typeparamref name="T"/>.
        /// </remarks>
        IQueryIndex<T> Any(params Action<IQueryIndex<T>>[] predicates);

        /// <summary>
        /// Keeps the records matching any of the predicates. A predicate filters the current records with <c>Where()</c>,
        /// or joins another index on their documents with <c>With()</c>, like <c>q =&gt; q.With&lt;PersonByAge&gt;(x =&gt; x.Age &lt; 18)</c>.
        /// </summary>
        /// <remarks>
        /// Each predicate joins its indexes with new aliases, so a record is only returned when its document has a record in each of them,
        /// and it repeats for each combination of their records, like with <see cref="With{TIndex}(Expression{Func{TIndex, bool}})"/>.
        /// The query still returns the records of <typeparamref name="T"/>.
        /// </remarks>
        ValueTask<IQueryIndex<T>> AnyAsync(params Func<IQueryIndex<T>, ValueTask>[] predicates);

        /// <summary>
        /// Keeps the records matching all the predicates. A predicate filters the current records with <c>Where()</c>,
        /// or joins another index on their documents with <c>With()</c>, like <c>q =&gt; q.With&lt;PersonByAge&gt;(x =&gt; x.Adult)</c>.
        /// </summary>
        /// <remarks>
        /// Each predicate joins its indexes with new aliases, so a record repeats for each combination of their matching records,
        /// like with <see cref="With{TIndex}(Expression{Func{TIndex, bool}})"/>. The query still returns the records of <typeparamref name="T"/>.
        /// </remarks>
        IQueryIndex<T> All(params Action<IQueryIndex<T>>[] predicates);

        /// <summary>
        /// Keeps the records matching all the predicates. A predicate filters the current records with <c>Where()</c>,
        /// or joins another index on their documents with <c>With()</c>, like <c>q =&gt; q.With&lt;PersonByAge&gt;(x =&gt; x.Adult)</c>.
        /// </summary>
        /// <remarks>
        /// Each predicate joins its indexes with new aliases, so a record repeats for each combination of their matching records,
        /// like with <see cref="With{TIndex}(Expression{Func{TIndex, bool}})"/>. The query still returns the records of <typeparamref name="T"/>.
        /// </remarks>
        ValueTask<IQueryIndex<T>> AllAsync(params Func<IQueryIndex<T>, ValueTask>[] predicates);

        /// <summary>
        /// Joins an index on the documents of the current records, and returns the records of the joined index.
        /// </summary>
        /// <remarks>
        /// A record is returned for each match of the joined indexes, so it repeats when another index has several records for its document.
        /// </remarks>
        IQueryIndex<TIndex> With<TIndex>() where TIndex : class, IIndex;

        /// <summary>
        /// Joins an index on the documents of the current records, filters it with a predicate, and returns the records of the joined index.
        /// </summary>
        /// <remarks>
        /// A record is returned for each match of the joined indexes, so it repeats when another index has several records for its document.
        /// </remarks>
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
        Task<T> FirstOrDefaultAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes the query.
        /// </summary>
        /// <remarks>
        /// The results are buffered in memory. Use <see cref="ToAsyncEnumerable(CancellationToken)"/> to stream them instead.
        /// </remarks>
        Task<IReadOnlyList<T>> ListAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes the query for asynchronous iteration.
        /// </summary>
        /// <returns></returns>
        IAsyncEnumerable<T> ToAsyncEnumerable(CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns the number of results only.
        /// </summary>
        Task<int> CountAsync(CancellationToken cancellationToken = default);
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

        /// <summary>
        /// Sets a descending OrderBy clause using a custom lambda expression.
        /// </summary>
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
