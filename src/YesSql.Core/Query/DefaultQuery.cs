using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Emit;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Transform;
using YesSql.Core.Data.Mappings;
using YesSql.Core.Data.Models;
using YesSql.Core.Indexes;
using Expression = System.Linq.Expressions.Expression;

namespace YesSql.Core.Query {
    public class DefaultQuery : IQuery {
        private readonly ISession _nhsession;
        private readonly Services.ISession _session;
        private readonly IDictionary<string, IQueryOver<Document>> _joins;
        private IQueryOver<Document, Document> _queryOver;

        public DefaultQuery(ISession nhsession, Services.ISession session)
        {
            _session = session;
            _nhsession = nhsession;

            _joins = new Dictionary<string, IQueryOver<Document>>();
        }

        public Services.ISession Session
        {
            get { return _session; }
        }

        private IQueryOver<Document, Document> BindDocument()
        {
            if(_queryOver == null)
            {
                _queryOver = _nhsession.QueryOver<Document>().TransformUsing(Transformers.DistinctRootEntity);
            }

            return _queryOver;
        }

        private IQueryOver<Document, TIndex> BindQueryOverByPath<TIndex>()
        {
            return BindQueryOverByPath<TIndex>(typeof (TIndex).Name);
        }

        private IQueryOver<Document, TIndex> BindQueryOverByPath<TIndex>(string name)
        {
            IQueryOver<Document> queryOver;
            if (_joins.TryGetValue(name, out queryOver)) 
            {
                return (IQueryOver<Document, TIndex>)queryOver;
            }

            // public IList<{TIndex}> {TIndex} {get;set;}
            var dynamicMethod = new DynamicMethod(typeof(TIndex).Name, typeof(IEnumerable<TIndex>), null, typeof(Document));
            var syntheticMethod = new SyntheticMethodInfo(dynamicMethod, typeof(Document));
            var syntheticProperty = new SyntheticPropertyInfo(syntheticMethod);

            // doc => doc.{TIndex}
            var parameter = Expression.Parameter(typeof(Document), "doc");
            var syntheticExpression = (Expression<Func<Document, IEnumerable<TIndex>>>)Expression.Lambda(
                typeof(Func<Document, IEnumerable<TIndex>>),
                Expression.Property(parameter, syntheticProperty),
                parameter);

            TIndex alias = default(TIndex);

            var join = BindDocument().JoinQueryOver(syntheticExpression, () => alias);
            _joins.Add(name, join);

            return join;
        }

        private void Where<TIndex>() where TIndex : IIndex
        {
            BindQueryOverByPath<TIndex>();
        }

        private void Where<TIndex>(Expression<Func<TIndex, bool>> predicate) where TIndex : IIndex
        {
            var queryOver = BindQueryOverByPath<TIndex>();
            queryOver.Where(predicate);
        }

        private void Where<TIndex>(ICriterion restriction) where TIndex : IIndex {
            var queryOver = BindQueryOverByPath<TIndex>();
            queryOver.Where(restriction);
        }

        private IEnumerable<Document> Slice(int skip, int count)
        {
            return BindDocument().Skip(skip).Take(count).List();
        }

        private void OrderBy<TIndex>(Expression<Func<TIndex, object>> keySelector) where TIndex : IIndex
        {
            BindQueryOverByPath<TIndex>().OrderBy(keySelector).Asc();
        }

        private void OrderByDescending<TIndex>(Expression<Func<TIndex, object>> keySelector) where TIndex : IIndex
        {
            BindQueryOverByPath<TIndex>().OrderBy(keySelector).Desc();
        }

        int Count()
        {
            return BindDocument().RowCount();
        }

        IQuery<TIndex> IQuery.For<TIndex>() {
            return new Query<TIndex>(this);
        }


        private T As<T>(Document doc) where T : class
        {
            return _session.As<T>(doc);
        }

        private IEnumerable<T> As<T>(IEnumerable<Document> doc) where T : class {
            return doc.Select(As<T>);
        }


        class Query<T> : IQuery<T> where T : class
        {
            protected readonly DefaultQuery _query;
            private int _skip;
            private int _count;

            public Query(DefaultQuery query) {
                _query = query;
            }

            public Services.ISession Session 
            {
                get { return _query.Session; }
            }

            public T FirstOrDefault()
            {
                return _query.As<T>(_query.Slice(0, 1)).FirstOrDefault();
            }

            IList<T> IQuery<T>.List()
            {
                if (_skip != 0 || _count != 0)
                {
                    return _query.As<T>(_query.Slice(_skip, _count)).ToList();
                }
                else
                {
                    return _query.As<T>(_query._queryOver.List()).ToList();
                }
            }

            IQuery<T> IQuery<T>.Skip(int count)
            {
                _skip = count;
                return this;
            }

            IQuery<T> IQuery<T>.Take(int count)
            {
                _count = count;
                return this;
            }


            int IQuery<T>.Count() 
            {
                return _query.Count();
            }

            IQuery<T, TIndex> IQuery<T>.With<TIndex>()
            {
                _query.Where<TIndex>();
                return new Query<T, TIndex>(_query);
            }

            IQuery<T, TIndex> IQuery<T>.With<TIndex>(Expression<Func<TIndex, bool>> predicate) 
            {
                _query.Where(predicate);
                return new Query<T, TIndex>(_query);
            }

            public IQuery<T> Where(Expression<Func<Document, bool>> predicate)
            {
                _query.BindDocument().Where(predicate);
                return this;
            }

            IQuery<T, TIndex> IQuery<T>.OrderBy<TIndex>(Expression<Func<TIndex, object>> keySelector) {
                _query.OrderBy(keySelector);
                return new Query<T, TIndex>(_query);
            }

            IQuery<T, TIndex> IQuery<T>.OrderByDescending<TIndex>(Expression<Func<TIndex, object>> keySelector) {
                _query.OrderByDescending(keySelector);
                return new Query<T, TIndex>(_query);
            }
        }


        class Query<T, TIndex> : Query<T>, IQuery<T, TIndex> 
            where T : class 
            where TIndex : IIndex
        {

            public Query(DefaultQuery query)
                : base(query) {
            }

            IQuery<T, TIndex> IQuery<T, TIndex>.Where(Expression<Func<TIndex, bool>> predicate) 
            {
                _query.Where(predicate);
                return this;
            }

            IQuery<T, TIndex> IQuery<T, TIndex>.Where(Func<Document, ICriterion> restriction)
            {
                Document alias = null;
                _query.Where<TIndex>(restriction(alias));
                return this;
            }

            IQuery<T, TIndex> IQuery<T, TIndex>.OrderBy<TKey>(Expression<Func<TIndex, object>> keySelector)
            {
                _query.OrderBy(keySelector);
                return this;
            }

            IQuery<T, TIndex> IQuery<T, TIndex>.OrderByDescending<TKey>(Expression<Func<TIndex, object>> keySelector)
            {
                _query.OrderByDescending(keySelector);
                return this;
            }
        }
    }
}
