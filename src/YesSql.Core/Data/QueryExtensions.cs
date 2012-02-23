using System;
using System.Linq.Expressions;
using YesSql.Core.Indexes;
using YesSql.Core.Query;
using YesSql.Core.Services;

namespace YesSql.Core.Data {
    public static class QueryExtensions {
        public static IQuery<T> Query<T>(this ISession session) where T : class {
            return session.Query().For<T>();
        }

        public static IQuery<T, TIndex> Query<T, TIndex>(this ISession session)
            where T : class
            where TIndex : IIndex {
            return session.Query().For<T>().With<TIndex>();
        }

        public static IQuery<T, TIndex> Query<T, TIndex>(this ISession session, Expression<Func<TIndex, bool>> predicate)
            where T : class
            where TIndex : IIndex {
            return session.Query().For<T>().With<TIndex>(predicate);
        }

    }
}
