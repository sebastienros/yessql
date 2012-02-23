using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using NHibernate.Criterion;
using YesSql.Core.Data.Models;
using YesSql.Core.Indexes;
using YesSql.Core.Services;

namespace YesSql.Core.Query {
    public interface IQuery {
        ISession Session { get; }
        IQuery<T> For<T>() where T : class;
    }

    public interface IQuery<T> where T : class {

        IQuery<T, TIndex> With<TIndex>() where TIndex : IIndex;
        IQuery<T, TIndex> With<TIndex>(Expression<Func<TIndex, bool>> predicate) where TIndex : IIndex;

        IQuery<T> Where(Expression<Func<Document, bool>> predicate);

        IQuery<T, TIndex> OrderBy<TIndex>(Expression<Func<TIndex, object>> keySelector) where TIndex : IIndex;
        IQuery<T, TIndex> OrderByDescending<TIndex>(Expression<Func<TIndex, object>> keySelector) where TIndex : IIndex;

        IQuery<T> Skip(int count);
        IQuery<T> Take(int count);
        T FirstOrDefault();
        IList<T> List();
        int Count();
    }

    public interface IQuery<T, TIndex> : IQuery<T>
        where T : class
        where TIndex : IIndex {

        IQuery<T, TIndex> Where(Expression<Func<TIndex, bool>> predicate);
        IQuery<T, TIndex> Where(Func<Document, ICriterion> restriction); 
        IQuery<T, TIndex> OrderBy<TKey>(Expression<Func<TIndex, object>> keySelector);
        IQuery<T, TIndex> OrderByDescending<TKey>(Expression<Func<TIndex, object>> keySelector);
    }
}
