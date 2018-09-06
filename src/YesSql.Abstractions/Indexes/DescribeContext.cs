using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace YesSql.Indexes
{
    public class DescribeContext<T> : IDescriptor
    {
        private readonly Dictionary<Type, IList<IDescribeFor>> _describes = new Dictionary<Type, IList<IDescribeFor>>();

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
                    IndexType = kp.IndexType
                });
        }

        public bool IsCompatibleWith(Type target)
        {
            return typeof(T).GetTypeInfo().IsAssignableFrom(target.GetTypeInfo());
        }

        public IMapFor<T, TIndex> For<TIndex>() where TIndex : IIndex
        {
            return For<TIndex, object>();
        }

        public IMapFor<T, TIndex> For<TIndex, TKey>() where TIndex : IIndex
        {
            IList<IDescribeFor> descriptors;
            if (!_describes.TryGetValue(typeof(T), out descriptors))
            {
                descriptors = _describes[typeof(T)] = new List<IDescribeFor>();
            }

            var describeFor = new IndexDescriptor<T, TIndex, TKey>();
            descriptors.Add(describeFor);

            return describeFor;
        }
    }
}
