using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using YesSql.Core.Indexes;

namespace YesSql.Core.Query {
    public interface IQuery {

        /// <summary>
        /// Adds a filter on the document type
        /// </summary>
        /// <typeparam name="T">The type of document to return</typeparam>
        IQuery<T> For<T>() where T : class;

        /// <summary>
        /// Defines what type of index should be returned
        /// </summary>
        /// <typeparam name="T">The type of index to return</typeparam>
        IQuery<T> ForIndex<T>() where T : Index;

        /// <summary>
        /// Returns documents from any type
        /// </summary>
        IQuery<object> Any();
    }

    public interface IQuery<T> where T : class {

        IQuery<T, TIndex> With<TIndex>() where TIndex : Index;
        IQuery<T, TIndex> With<TIndex>(Expression<Func<TIndex, bool>> predicate) where TIndex : Index;
        IQuery<T> OrderBy(Expression<Func<T, object>> keySelector);
        IQuery<T> OrderByDescending(Expression<Func<T, object>> keySelector) ;
        IQuery<T> ThenBy(Expression<Func<T, object>> keySelector) ;
        IQuery<T> ThenByDescending(Expression<Func<T, object>> keySelector) ;

        IQuery<T> Skip(int count);
        IQuery<T> Take(int count);
        Task<T> FirstOrDefault();
        Task<IEnumerable<T>> List();
        Task<int> Count();
    }

    public interface IQuery<T, TIndex> : IQuery<T>
        where T : class
        where TIndex : Index
    {
        IQuery<T, TIndex> Where(Expression<Func<TIndex, bool>> predicate);
        IQuery<T, TIndex> Where(string sql);
        IQuery<T, TIndex> OrderBy<TKey>(Expression<Func<TIndex, object>> keySelector);
        IQuery<T, TIndex> OrderByDescending<TKey>(Expression<Func<TIndex, object>> keySelector);
        IQuery<T, TIndex> ThenBy<TKey>(Expression<Func<TIndex, object>> keySelector);
        IQuery<T, TIndex> ThenByDescending<TKey>(Expression<Func<TIndex, object>> keySelector);
    }
}
