using System;
using System.Linq.Expressions;
using YesSql.Indexes;

namespace YesSql
{
    public static class QueryExtensions
    {
        public static IQuery<T> Query<T>(this ISession session, string collection = null) where T : class
        {
            return session.Query(collection).For<T>();
        }

        public static IQueryIndex<TIndex> QueryIndex<TIndex>(this ISession session, string collection = null) where TIndex : class, IIndex
        {
            return session.Query(collection).ForIndex<TIndex>();
        }

        public static IQueryIndex<TIndex> QueryIndex<TIndex>(this ISession session, Expression<Func<TIndex, bool>> predicate, string collection = null) where TIndex : class, IIndex
        {
            return session.Query(collection).ForIndex<TIndex>().Where(predicate);
        }

        public static IQuery<T, TIndex> Query<T, TIndex>(this ISession session, bool filterType = false, string collection = null)
            where T : class
            where TIndex : class, IIndex
        {
            return session.Query(collection).For<T>(filterType).With<TIndex>();
        }

        public static IQuery<T, TIndex> Query<T, TIndex>(this ISession session, string collection)
            where T : class
            where TIndex : class, IIndex
        {
            return session.Query<T, TIndex>(false, collection);
        }

        public static IQuery<T, TIndex> Query<T, TIndex>(this ISession session, Expression<Func<TIndex, bool>> predicate, bool filterType = false, string collection = null)
            where T : class
            where TIndex : class, IIndex
        {
            return session.Query(collection).For<T>(filterType).With<TIndex>(predicate);
        }

        public static IQuery<T, TIndex> Query<T, TIndex>(this ISession session, Expression<Func<TIndex, bool>> predicate, string collection)
            where T : class
            where TIndex : class, IIndex
        {
            return session.Query<T, TIndex>(predicate, false, collection);
        }

    }
}
