using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System;
using YesSql.Indexes;
using YesSql.Tests.Models;

namespace YesSql.Tests.Indexes
{
    public class PropertyIndex : MapIndex
    {
        public string Name { get; set; }
        public bool ForRent { get; set; }
        public bool IsOccupied { get; set; }
        public string Location { get; set; }
    }

    //public class PropertyIndexProvider : DynamicIndexProviderBase<Property>
    public class PropertyIndexProvider : IndexProvider<Property>
    {
        public override void Describe(DescribeContext<Property> context)
        {
            context
                .For(typeof(PropertyIndex), property =>
                {

                    var idx = new PropertyIndex
                    {
                        Name = property.Name,
                        ForRent = property.ForRent,
                        IsOccupied = property.IsOccupied,
                        Location = property.Location

                    };
                    return Task.FromResult(new[] { (IIndex)idx }.AsEnumerable());
                });
        }
    }


    //public abstract class DynamicIndexProviderBase<T> : IIndexProvider
    //{
    //    public DynamicIndexProviderBase(string collectionName)
    //    {
    //        CollectionName = collectionName;
    //    }
    //    public DynamicIndexProviderBase()
    //    {
    //        CollectionName = string.Empty;
    //    }

    //    public abstract void Describe(DynamicDescribeContext<T> context);

    //    void IIndexProvider.Describe(IDescriptor context)
    //    {
    //        var dynamicDescribeContext = new DynamicDescribeContext<T>();
    //        //var defaultDescriptor = (DescribeContext<T>)context;
    //        Describe(dynamicDescribeContext); //Yessql 需要改进 Store 中 DescriptorActivators 的获取方式 CreateDescriptors
    //    }

    //    public string CollectionName { get; set; }

    //    public Type ForType()
    //    {
    //        return typeof(T);
    //    }
    //}

    //public class DynamicDescribeContext<T> : IDescriptor
    //{
    //    private readonly Dictionary<Type, List<IDescribeFor>> _describes = new Dictionary<Type, List<IDescribeFor>>();

    //    public IEnumerable<IndexDescriptor> Describe(params Type[] types)
    //    {
    //        return _describes
    //            .Where(kp => types == null || types.Length == 0 || types.Contains(kp.Key))
    //            .SelectMany(x => x.Value)
    //            .Select(kp => new IndexDescriptor
    //            {
    //                Type = kp.IndexType,
    //                Map = kp.GetMap(),
    //                Reduce = kp.GetReduce(),
    //                Delete = kp.GetDelete(),
    //                GroupKey = kp.GroupProperty,
    //                IndexType = kp.IndexType,
    //                Filter = kp.Filter
    //            });
    //    }



    //    public void For(Type indexType, Func<T, Task<IEnumerable<IIndex>>> mapfn)
    //    {
    //        List<IDescribeFor> descriptors;

    //        if (!_describes.TryGetValue(typeof(T), out descriptors))
    //        {
    //            descriptors = _describes[typeof(T)] = new List<IDescribeFor>();
    //        }
    //        var contextType = typeof(DynamicIndexDescriptor<,>).MakeGenericType(typeof(T), indexType);
    //        var describeFor = (IDescribeFor)Activator.CreateInstance(contextType, mapfn);
    //        //var constrctor = contextType.GetConstructor(new Type[] { typeof(Func<T, Task<IEnumerable<IIndex>>>) });
    //        /*Expression.Lambda<IDescribeFor>(Expression.New(constrctor, Expression. )).Compile();*/

    //        descriptors.Add(describeFor);

    //    }
    //}

}
