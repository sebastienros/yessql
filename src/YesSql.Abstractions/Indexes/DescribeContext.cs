using System;
using System.Collections.Generic;
using System.Linq;
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
        public void For(Type indexType, Func<T, Task<IEnumerable<IIndex>>> mapfn)
        {
            List<IDescribeFor> descriptors;

            if (!_describes.TryGetValue(typeof(T), out descriptors))
            {
                descriptors = _describes[typeof(T)] = new List<IDescribeFor>();
            }
            var contextType = typeof(IndexDescriptor<,,>).MakeGenericType(typeof(T), indexType, typeof(object));
            var describeFor = Activator.CreateInstance(contextType);
            var mapMethod = contextType.GetMethods().Where(x => x.Name == "Map" && x.GetParameters().FirstOrDefault().ParameterType == mapfn.GetType()).FirstOrDefault();
            mapMethod.Invoke(describeFor, new[] { mapfn });
            descriptors.Add((IDescribeFor)describeFor);
        }

        public IMapFor<T, TIndex> For<TIndex>() where TIndex : IIndex
        {
            return For<TIndex, object>();
        }

        public IMapFor<T, TIndex> For<TIndex, TKey>() where TIndex : IIndex
        {
            List<IDescribeFor> descriptors;

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
