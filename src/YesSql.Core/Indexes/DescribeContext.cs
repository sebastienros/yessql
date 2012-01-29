using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace YesSql.Core.Indexes {
    public class DescribeContext 
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
                    Update = kp.GetUpdate(),
                    GroupKey = kp.GroupProperty,
                    IndexType = kp.IndexType
                });
        }

        public DescribeFor<T, TIndex, object> For<T, TIndex>() where TIndex : IIndex
        {
            return For<T, TIndex, object>();
        }

        public DescribeFor<T, TIndex, TKey> For<T, TIndex, TKey>() where TIndex : IIndex 
        {
            IList<IDescribeFor> descriptors;
            if (!_describes.TryGetValue(typeof(T), out descriptors)) {
                descriptors = _describes[typeof(T)] = new List<IDescribeFor>();
            }

            var describeFor = new DescribeFor<T, TIndex, TKey>();
            descriptors.Add(describeFor);

            var groupProperties = typeof(TIndex).GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(
                x => x.GetCustomAttributes(typeof(GroupKeyAttribute), true).Any())
                .ToArray();

            if(groupProperties.Count() > 1)
            {
                throw new InvalidOperationException("There should be only one GroupKey attribute defined: " + typeof(TIndex).FullName);
            }

            describeFor.GroupProperty = groupProperties.SingleOrDefault();

            return describeFor;
        }

    }
}