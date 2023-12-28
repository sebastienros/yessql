using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace YesSql.Indexes
{
    public class DescribeContext<T> : IDescriptor
    {
        private readonly Dictionary<Type, List<IDescribeFor>> _describes = new Dictionary<Type, List<IDescribeFor>>();

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
                    Filter = kp.Filter
                });
        }


        public IMapFor<T, TIndex> For<TIndex>(Type indexType = null) where TIndex : IIndex
        {
            return For<TIndex, object>(indexType);
        }

        public IMapFor<T, TIndex> For<TIndex, TKey>(Type indexType = null) where TIndex : IIndex
        {
            List<IDescribeFor> descriptors;

            if (!_describes.TryGetValue(typeof(T), out descriptors))
            {
                descriptors = _describes[typeof(T)] = new List<IDescribeFor>();
            }

            var describeFor = new IndexDescriptor<T, TIndex, TKey>();
            if (indexType != null)
            {
                describeFor.IndexType = indexType;
            }
            descriptors.Add(describeFor);

            return describeFor;
        }
    }
}
