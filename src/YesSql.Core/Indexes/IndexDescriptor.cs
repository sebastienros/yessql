using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace YesSql.Core.Indexes
{
    public class IndexDescriptor
    {
        public Type Type { get; set; }
        public Func<object, IEnumerable<Index>> Map { get; set; }
        public Func<IGrouping<object, Index>, Index> Reduce { get; set; }
        public Func<Index, IEnumerable<Index>, Index> Update { get; set; }
        public Func<Index, IEnumerable<Index>, Index> Delete { get; set; }
        public PropertyInfo GroupKey { get; set; }
        public Type IndexType { get; set; }
    }
}