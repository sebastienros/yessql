using System;
using System.Collections.Generic;

namespace YesSql.Indexes
{
    public interface IDescriptor
    {
        IEnumerable<IndexDescriptor> Describe(params Type[] types);
        bool IsCompatibleWith(Type target);
    }
}