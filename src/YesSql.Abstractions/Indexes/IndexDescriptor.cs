using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace YesSql.Indexes
{
    public class IndexDescriptor
    {
        public Type Type { get; set; }
        public Func<object, Task<IEnumerable<IIndex>>> Map { get; set; }
        public Func<IGrouping<object, IIndex>, IIndex> Reduce { get; set; }
        public Func<IIndex, IEnumerable<IIndex>, IIndex> Update { get; set; }
        public Func<IIndex, IEnumerable<IIndex>, IIndex> Delete { get; set; }
        public PropertyInfo GroupKey { get; set; }
        public Type IndexType { get; set; }
    }
}