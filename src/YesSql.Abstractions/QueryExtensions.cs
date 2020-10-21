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
        public static IQuery<T> Query<T>(this ISession session, string collection = null) where T : class
        {
            return session.Query(collection).For<T>();
        }

        /// <summary>
        /// Creates a query on an index.
        /// </summary>
        public static IQueryIndex<TIndex> QueryIndex<TIndex>(this ISession session, string collection = null) where TIndex : class, IIndex
        {
            return session.Query(collection).ForIndex<TIndex>();
        }

        /// <summary>
        /// Creates a query on an index, with a predicate.
        /// </summary>
        public static IQueryIndex<TIndex> QueryIndex<TIndex>(this ISession session, Expression<Func<TIndex, bool>> predicate, string collection = null) where TIndex : class, IIndex
        {
            return session.Query(collection).ForIndex<TIndex>().Where(predicate);
        }

        /// <summary>
        /// Creates a query for a type, using an index.
        /// </summary>
        public static IQuery<T, TIndex> Query<T, TIndex>(this ISession session, bool filterType = false, string collection = null)
            where T : class
            where TIndex : class, IIndex
        {
            return session.Query(collection).For<T>(filterType).With<TIndex>();
        }

        /// <summary>
        /// Creates a query for a type, using an index.
        /// </summary>
        public static IQuery<T, TIndex> Query<T, TIndex>(this ISession session, string collection)
            where T : class
            where TIndex : class, IIndex
        {
            return session.Query<T, TIndex>(false, collection);
        }

        /// <summary>
        /// Creates a query for a type, using an index.
        /// </summary>
        public static IQuery<T, TIndex> Query<T, TIndex>(this ISession session, Expression<Func<TIndex, bool>> predicate, bool filterType = false, string collection = null)
            where T : class
            where TIndex : class, IIndex
        {
            return session.Query(collection).For<T>(filterType).With<TIndex>(predicate);
        }

        /// <summary>
        /// Creates a query for a type, using an index.
        /// </summary>
        public static IQuery<T, TIndex> Query<T, TIndex>(this ISession session, Expression<Func<TIndex, bool>> predicate, string collection)
            where T : class
            where TIndex : class, IIndex
        {
            return session.Query<T, TIndex>(predicate, false, collection);
        }
    }
}
