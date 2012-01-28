using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace YesSql.Core.Indexes
{
    public interface IDescribeFor
    {
        Func<IEnumerable<object>, IEnumerable<IIndex>> GetMap();
        Func<IGrouping<object, IIndex>, IIndex> GetReduce();
        Func<IIndex, IEnumerable<IIndex>, IIndex> GetUpdate();
        Func<IIndex, IEnumerable<IIndex>, IIndex> GetDelete();
        PropertyInfo GroupProperty { get; set; }
        Type IndexType { get; }
    }

    public class DescribeFor<T, TIndex, TKey> : IDescribeFor where TIndex : IIndex
    {
        public Func<IEnumerable<T>, IEnumerable<TIndex>> Map { get; private set; }
        public Func<IGrouping<TKey, TIndex>, TIndex> Reduce { get; private set; }
        public Func<TIndex, IEnumerable<TIndex>, TIndex> Delete { get; private set; }
        public Func<TIndex, IEnumerable<TIndex>, TIndex> Update { get; private set; }
        public PropertyInfo GroupProperty { get; set; }
        public Type IndexType { get { return typeof (TIndex); } }

        public void Index(Func<IEnumerable<T>, IEnumerable<TIndex>> map)
        {
            Index(map, null);
        }

        public void Index(Func<IEnumerable<T>, IEnumerable<TIndex>> map,
                          Func<IGrouping<TKey, TIndex>, TIndex> reduce,
                          Func<TIndex, IEnumerable<TIndex>, TIndex> delete = null,
                          Func<TIndex, IEnumerable<TIndex>, TIndex> update = null) {
            Map = map;
            Reduce = reduce;
            Delete = delete;
            Update = update;
        }

        Func<IEnumerable<object>, IEnumerable<IIndex>> IDescribeFor.GetMap()
        {
            return x => Map(x.Cast<T>()).Cast<IIndex>();
        }

        Func<IGrouping<object, IIndex>, IIndex> IDescribeFor.GetReduce()
        {
            if (Reduce == null)
            {
                return null;
            }

            return x =>
            {
                var grouping = new GroupedEnumerable<TKey, TIndex>(x.Key, x);
                return Reduce(grouping);
            };
        }

        Func<IIndex, IEnumerable<IIndex>, IIndex> IDescribeFor.GetDelete() {
            return (index, obj) => Delete((TIndex) index, obj.Cast<TIndex>());
        }

        Func<IIndex, IEnumerable<IIndex>, IIndex> IDescribeFor.GetUpdate() {
            return (index, obj) => Update((TIndex) index, obj.Cast<TIndex>());
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