using System;
using System.Linq.Expressions;
using YesSql.Core.Indexes;
using YesSql.Core.Query;

namespace YesSql.Core.Services {
    public static class QueryExtensions
    {
        public static IQuery<T> QueryAsync<T>(this ISession session) where T : class
        {
            return session.QueryAsync().For<T>();
        }

        public static IQueryIndex<TIndex> QueryIndexAsync<TIndex>(this ISession session) where TIndex : class, IIndex
        {
            return session.QueryAsync().ForIndex<TIndex>();
        }

        public static IQueryIndex<TIndex> QueryIndexAsync<TIndex>(this ISession session, Expression<Func<TIndex, bool>> predicate) where TIndex : class, IIndex
        {
            return session.QueryAsync().ForIndex<TIndex>().Where(predicate);
        }

        public static IQuery<T, TIndex> QueryAsync<T, TIndex>(this ISession session, bool filterType = false)
            where T : class
            where TIndex : class, IIndex
        {
            return session.QueryAsync().For<T>(filterType).With<TIndex>();
        }

        public static IQuery<T, TIndex> QueryAsync<T, TIndex>(this ISession session, Expression<Func<TIndex, bool>> predicate, bool filterType = false)
            where T : class
            where TIndex : class, IIndex
        {
            return session.QueryAsync().For<T>(filterType).With<TIndex>(predicate);
        }

    }
}
