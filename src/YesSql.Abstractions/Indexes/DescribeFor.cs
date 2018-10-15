using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace YesSql.Indexes
{
    public interface IDescribeFor
    {
        Func<object, Task<IEnumerable<IIndex>>> GetMap();
        Func<IGrouping<object, IIndex>, IIndex> GetReduce();
        Func<IIndex, IEnumerable<IIndex>, IIndex> GetDelete();
        PropertyInfo GroupProperty { get; set; }
        Type IndexType { get; }
    }

    public interface IMapFor<out T, TIndex> where TIndex : IIndex
    {
        IGroupFor<TIndex> Map(Func<T, TIndex> map);
        IGroupFor<TIndex> Map(Func<T, IEnumerable<TIndex>> map);
        IGroupFor<TIndex> Map(Func<T, Task<TIndex>> map);
        IGroupFor<TIndex> Map(Func<T, Task<IEnumerable<TIndex>>> map);
    }

    public interface IGroupFor<TIndex> where TIndex : IIndex
    {
        IReduceFor<TIndex, TKey> Group<TKey>(Expression<Func<TIndex, TKey>> group);
    }

    public interface IReduceFor<TIndex, out TKey> where TIndex : IIndex
    {
        IDeleteFor<TIndex> Reduce(Func<IGrouping<TKey, TIndex>, TIndex> reduce);
    }

    public interface IDeleteFor<TIndex> where TIndex : IIndex
    {
        void Delete(Func<TIndex, IEnumerable<TIndex>, TIndex> delete = null);
    }

    public class IndexDescriptor<T, TIndex, TKey> : IDescribeFor, IMapFor<T, TIndex>, IGroupFor<TIndex>, IReduceFor<TIndex, TKey>, IDeleteFor<TIndex> where TIndex : IIndex
    {
        private Func<T, Task<IEnumerable<TIndex>>> _map;
        private Func<IGrouping<TKey, TIndex>, TIndex> _reduce;
        private Func<TIndex, IEnumerable<TIndex>, TIndex> _delete;
        private IDescribeFor _reduceDescribeFor;

        public PropertyInfo GroupProperty { get; set; }
        public Type IndexType { get { return typeof(TIndex); } }

        public IGroupFor<TIndex> Map(Func<T, IEnumerable<TIndex>> map)
        {
            _map = x => Task.FromResult(map(x));
            return this;
        }

        public IGroupFor<TIndex> Map(Func<T, TIndex> map)
        {
            _map = x => Task.FromResult((IEnumerable<TIndex>) new[] { map(x) });
            return this;
        }

        public IGroupFor<TIndex> Map(Func<T, Task<IEnumerable<TIndex>>> map)
        {
            _map = map;
            return this;
        }

        public IGroupFor<TIndex> Map(Func<T, Task<TIndex>> map)
        {
            _map = async x => new[] { await map(x) };
            return this;
        }

        public IReduceFor<TIndex, TKeyG> Group<TKeyG>(Expression<Func<TIndex, TKeyG>> group)
        {
            var memberExpression = group.Body as MemberExpression;

            if (memberExpression == null)
            {
                throw new ArgumentException("Group expression is not a valid member of: " + typeof(TIndex).Name);
            }

            var property = memberExpression.Member as PropertyInfo;

            if (property == null)
            {
                throw new ArgumentException("Group expression is not a valid property of: " + typeof(TIndex).Name);
            }

            GroupProperty = property;

            var reduceDescibeFor = new IndexDescriptor<T, TIndex, TKeyG>();
            _reduceDescribeFor = reduceDescibeFor;

            return reduceDescibeFor;
        }

        public IDeleteFor<TIndex> Reduce(Func<IGrouping<TKey, TIndex>, TIndex> reduce)
        {
            _reduce = reduce;
            return this;
        }

        public void Delete(Func<TIndex, IEnumerable<TIndex>, TIndex> delete = null)
        {
            _delete = delete;
        }

        Func<object, Task<IEnumerable<IIndex>>> IDescribeFor.GetMap()
        {
            return async x => (await _map((T)x) ?? Enumerable.Empty<TIndex>()).Cast<IIndex>();
        }

        Func<IGrouping<object, IIndex>, IIndex> IDescribeFor.GetReduce()
        {
            if (_reduceDescribeFor != null)
            {
                return _reduceDescribeFor.GetReduce();
            }

            if (_reduce == null)
            {
                return null;
            }

            return x =>
            {
                var grouping = new GroupedEnumerable<TKey, TIndex>(x.Key, x);
                return _reduce(grouping);
            };
        }

        Func<IIndex, IEnumerable<IIndex>, IIndex> IDescribeFor.GetDelete()
        {
            if (_reduceDescribeFor != null)
            {
                return _reduceDescribeFor.GetDelete();
            }

            return (index, obj) => _delete((TIndex)index, obj.Cast<TIndex>());
        }

    }

    public class GroupedEnumerable<TKey, TIndex> : IGrouping<TKey, TIndex> where TIndex : IIndex
    {
        private readonly object _key;
        private readonly IEnumerable<IIndex> _enumerable;

        public GroupedEnumerable(object key, IEnumerable<IIndex> enumerable)
        {
            _key = key;
            _enumerable = enumerable;
        }

        public TKey Key
        {
            get { return (TKey)_key; }
        }

        public IEnumerator<TIndex> GetEnumerator()
        {
            return _enumerable.Cast<TIndex>().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}