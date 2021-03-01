using System;
using System.Linq.Expressions;
using YesSql.Indexes;

namespace YesSql
{
    public static class QueryExtensions
    {
        /// <summary>
        /// Creates a query on object of a type.
        /// </summary>
        public static IQuery<T> Query<T>(this ISessionReadOnly sessionReadOnly, string collection = null) where T : class
        {
            return sessionReadOnly.Query(collection).For<T>();
        }

        /// <summary>
        /// Creates a query on an index.
        /// </summary>
        public static IQueryIndex<TIndex> QueryIndex<TIndex>(this ISessionReadOnly sessionReadOnly, string collection = null) where TIndex : class, IIndex
        {
            return sessionReadOnly.Query(collection).ForIndex<TIndex>();
        }

        /// <summary>
        /// Creates a query on an index, with a predicate.
        /// </summary>
        public static IQueryIndex<TIndex> QueryIndex<TIndex>(this ISessionReadOnly sessionReadOnly, Expression<Func<TIndex, bool>> predicate, string collection = null) where TIndex : class, IIndex
        {
            return sessionReadOnly.Query(collection).ForIndex<TIndex>().Where(predicate);
        }

        /// <summary>
        /// Creates a query for a type, using an index.
        /// </summary>
        public static IQuery<T, TIndex> Query<T, TIndex>(this ISessionReadOnly sessionReadOnly, bool filterType = false, string collection = null)
            where T : class
            where TIndex : class, IIndex
        {
            return sessionReadOnly.Query(collection).For<T>(filterType).With<TIndex>();
        }

        /// <summary>
        /// Creates a query for a type, using an index.
        /// </summary>
        public static IQuery<T, TIndex> Query<T, TIndex>(this ISessionReadOnly sessionReadOnly, string collection)
            where T : class
            where TIndex : class, IIndex
        {
            return sessionReadOnly.Query<T, TIndex>(false, collection);
        }

        /// <summary>
        /// Creates a query for a type, using an index.
        /// </summary>
        public static IQuery<T, TIndex> Query<T, TIndex>(this ISessionReadOnly sessionReadOnly, Expression<Func<TIndex, bool>> predicate, bool filterType = false, string collection = null)
            where T : class
            where TIndex : class, IIndex
        {
            return sessionReadOnly.Query(collection).For<T>(filterType).With<TIndex>(predicate);
        }

        /// <summary>
        /// Creates a query for a type, using an index.
        /// </summary>
        public static IQuery<T, TIndex> Query<T, TIndex>(this ISessionReadOnly sessionReadOnly, Expression<Func<TIndex, bool>> predicate, string collection)
            where T : class
            where TIndex : class, IIndex
        {
            return sessionReadOnly.Query<T, TIndex>(predicate, false, collection);
        }
    }
}
