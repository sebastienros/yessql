using System;
using System.Collections.Generic;
using System.Linq;

namespace YesSql.Indexes
{
    public class DescribeContext<T> : IDescriptor
    {
        private readonly Dictionary<Type, List<IDescribeFor>> _describes = new Dictionary<Type, List<IDescribeFor>>();
        private Func<object, bool> _filter;

        public IEnumerable<IndexDescriptor> Describe(params Type[] types)
        {
            return _describes
                .Where(kp => types == null || types.Length == 0 || types.Contains(kp.Key))
                .SelectMany(x => x.Value)
                .Select(kp => new IndexDescriptor
                {
                    Type = kp.IndexType,
                    Map = kp.GetMap(),
                    Reduce = kp.GetReduce(),
                    Delete = kp.GetDelete(),
                    GroupKey = kp.GroupProperty,
                    IndexType = kp.IndexType,
                    Filter = _filter
                });
        }

        public IMapFor<T, TIndex> For<TIndex>(Func<T, bool> predicate = null) where TIndex : IIndex
        {
            return For<TIndex, object>(predicate);
        }

        public IMapFor<T, TIndex> For<TIndex, TKey>(Func<T, bool> predicate = null) where TIndex : IIndex
        {
            List<IDescribeFor> descriptors;

            if (!_describes.TryGetValue(typeof(T), out descriptors))
            {
                descriptors = _describes[typeof(T)] = new List<IDescribeFor>();
            }

            var describeFor = new IndexDescriptor<T, TIndex, TKey>();
            descriptors.Add(describeFor);

            if (predicate != null)
            {
                _filter = s => predicate((T)s);
            }

            return describeFor;
        }
    }
}
