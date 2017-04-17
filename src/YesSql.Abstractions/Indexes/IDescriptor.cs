using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace YesSql.Indexes
{
    public interface IDescriptor
    {
        IEnumerable<IndexDescriptor> Describe(params Type[] types);
        bool IsCompatibleWith(Type target);
    }
}