using System;
using System.Linq.Expressions;
using YesSql.Indexes;

namespace YesSql
{
    public static class QueryExtensions
    {
        public static IQuery<T> Query<T>(this ISession session) where T : class
        {
            return session.Query().For<T>();
        }

        public static IQueryIndex<TIndex> QueryIndex<TIndex>(this ISession session) where TIndex : class, IIndex
        {
            return session.Query().ForIndex<TIndex>();
        }

        public static IQueryIndex<TIndex> QueryIndex<TIndex>(this ISession session, Expression<Func<TIndex, bool>> predicate) where TIndex : class, IIndex
        {
            return session.Query().ForIndex<TIndex>().Where(predicate);
        }

        public static IQuery<T, TIndex> Query<T, TIndex>(this ISession session, bool filterType = false)
            where T : class
            where TIndex : class, IIndex
        {
            return session.Query().For<T>(filterType).With<TIndex>();
        }

        public static IQuery<T, TIndex> Query<T, TIndex>(this ISession session, Expression<Func<TIndex, bool>> predicate, bool filterType = false)
            where T : class
            where TIndex : class, IIndex
        {
            return session.Query().For<T>(filterType).With<TIndex>(predicate);
        }

    }
}
