using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace YesSql.Core.Indexes
{
    public interface IDescribeFor
    {
        Func<object, IEnumerable<IIndex>> GetMap();
        Func<IGrouping<object, IIndex>, IIndex> GetReduce();
        Func<IIndex, IEnumerable<IIndex>, IIndex> GetDelete();
        PropertyInfo GroupProperty { get; set; }
        Type IndexType { get; }
    }

    public interface IConfigureFor<out T, TIndex, out TKey> where TIndex : IIndex
    {
        IConfigureFor<T, TIndex, TKey> Map(Func<T, TIndex> map);
        IConfigureFor<T, TIndex, TKey> Map(Func<T, IEnumerable<TIndex>> map);
        IConfigureFor<T, TIndex, TKey> Reduce(Func<IGrouping<TKey, TIndex>, TIndex> reduce);
        IConfigureFor<T, TIndex, TKey> Delete(Func<TIndex, IEnumerable<TIndex>, TIndex> delete = null);
    }

    public class DescribeFor<T, TIndex, TKey> : IDescribeFor, IConfigureFor<T, TIndex, TKey> where TIndex : IIndex
    {
        private Func<T, IEnumerable<TIndex>> _map;
        private Func<IGrouping<TKey, TIndex>, TIndex> _reduce;
        private Func<TIndex, IEnumerable<TIndex>, TIndex> _delete;
        public PropertyInfo GroupProperty { get; set; }
        public Type IndexType { get { return typeof (TIndex); } }

        public IConfigureFor<T, TIndex, TKey> Map(Func<T, IEnumerable<TIndex>> map) 
        {
            _map = map;
            return this;
        }

        public IConfigureFor<T, TIndex, TKey> Map(Func<T, TIndex> map)
        {
            _map = x => new [] { map(x) };
            return this;
        }

        public IConfigureFor<T, TIndex, TKey> Reduce(Func<IGrouping<TKey, TIndex>, TIndex> reduce) 
        {
            _reduce = reduce;
            return this;
        }

        public IConfigureFor<T, TIndex, TKey> Delete(Func<TIndex, IEnumerable<TIndex>, TIndex> delete = null) 
        {
            _delete = delete;
            return this;
        }

        Func<object, IEnumerable<IIndex>> IDescribeFor.GetMap()
        {
            return x => _map((T)x).Cast<IIndex>();
        }

        Func<IGrouping<object, IIndex>, IIndex> IDescribeFor.GetReduce()
        {
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

        Func<IIndex, IEnumerable<IIndex>, IIndex> IDescribeFor.GetDelete() {
            return (index, obj) => _delete((TIndex) index, obj.Cast<TIndex>());
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