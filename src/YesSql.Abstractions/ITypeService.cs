using System;
using System.Collections.Generic;

namespace YesSql
{
    public interface ITypeService
    {
        string this[Type t] { get; set; }

        IEnumerable<Type> Keys { get; }

        IEnumerable<string> Values { get; }

        Type ReverseLookup(string value);
    }
}
