using System;
using System.Linq.Expressions;
using YesSql.Core.Indexes;
using YesSql.Core.Query;
using YesSql.Core.Services;

namespace YesSql.Core.Services {
    public static class QueryExtensions
    {
        public static IQuery<T> QueryAsync<T>(this ISession session) where T : class
        {
            return session.QueryAsync().For<T>();
        }

        public static IQueryIndex<TIndex> QueryIndexAsync<TIndex>(this ISession session) where TIndex : Index
        {
            return session.QueryAsync().ForIndex<TIndex>();
        }

        public static IQueryIndex<TIndex> QueryIndexAsync<TIndex>(this ISession session, Expression<Func<TIndex, bool>> predicate) where TIndex : Index
        {
            return session.QueryAsync().ForIndex<TIndex>().With<TIndex>(predicate);
        }

        public static IQuery<T, TIndex> QueryAsync<T, TIndex>(this ISession session)
            where T : class
            where TIndex : Index
        {
            return session.QueryAsync().For<T>().With<TIndex>();
        }

        public static IQuery<T, TIndex> QueryAsync<T, TIndex>(this ISession session, Expression<Func<TIndex, bool>> predicate)
            where T : class
            where TIndex : Index
        {
            return session.QueryAsync().For<T>().With<TIndex>(predicate);
        }

    }
}
