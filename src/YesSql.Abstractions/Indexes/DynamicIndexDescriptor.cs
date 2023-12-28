using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace YesSql.Indexes
{
    public class DynamicIndexDescriptor<T, TIndex> :  IDescribeFor where TIndex : IIndex
    {
        private Func<T, Task<IEnumerable<IIndex>>> _map;
        public DynamicIndexDescriptor(Func<T, Task<IEnumerable<IIndex>>> mapfn)
        {
            _map = mapfn;
        }

        private Func<TIndex, IEnumerable<TIndex>, TIndex> _delete;
        private IDescribeFor _reduceDescribeFor;
        private Func<object, bool> _filter;


        public PropertyInfo GroupProperty { get; set; }

        public Type IndexType => typeof(TIndex);

        public Func<object, bool> Filter => throw new NotImplementedException();

        Func<IIndex, IEnumerable<IIndex>, IIndex> IDescribeFor.GetDelete()
        {
            if (_reduceDescribeFor != null)
            {
                return _reduceDescribeFor.GetDelete();
            }

            return (index, obj) => _delete((TIndex)index, obj.Cast<TIndex>());
        }

        Func<object, Task<IEnumerable<IIndex>>> IDescribeFor.GetMap()
        {
            return async x => (await _map((T)x) ?? Enumerable.Empty<IIndex>()).Cast<IIndex>();
        }

        public Func<IGrouping<object, IIndex>, IIndex> GetReduce()
        {
            return null;
        }

    }

}
