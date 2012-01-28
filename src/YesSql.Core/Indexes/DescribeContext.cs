using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace YesSql.Core.Indexes {
    public class DescribeContext 
    {
        private readonly Dictionary<Type, IDescribeFor> _describes = new Dictionary<Type, IDescribeFor>();

        public IEnumerable<IndexDescriptor> Describe(params Type[] types)
        {
            return _describes
                .Where(kp => types == null || types.Length == 0 || types.Contains(kp.Key))
                .Select(kp => new IndexDescriptor
                {
                    Type = kp.Key,
                    Map = kp.Value.GetMap(),
                    Reduce = kp.Value.GetReduce(),
                    Delete = kp.Value.GetDelete(),
                    Update = kp.Value.GetUpdate(),
                    GroupKey = kp.Value.GroupProperty,
                    IndexType = kp.Value.IndexType
                });
        }

        public DescribeFor<T, TIndex, object> For<T, TIndex>() where TIndex : IIndex
        {
            return For<T, TIndex, object>();
        }

        public DescribeFor<T, TIndex, TKey> For<T, TIndex, TKey>() where TIndex : IIndex 
        {
            IDescribeFor describeFor;
            if (!_describes.TryGetValue(typeof(T), out describeFor)) {
                describeFor = new DescribeFor<T, TIndex, TKey>();
                _describes[typeof(T)] = describeFor;
            }

            var groupProperties = typeof(TIndex).GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(
                x => x.GetCustomAttributes(typeof(GroupKeyAttribute), true).Any())
                .ToArray();

            if(groupProperties.Count() > 1)
            {
                throw new InvalidOperationException("There should be only one GroupKey attribute defined: " + typeof(TIndex).FullName);
            }

            describeFor.GroupProperty = groupProperties.SingleOrDefault();

            return (DescribeFor<T, TIndex, TKey>)describeFor;
        }

    }
}