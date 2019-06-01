using System;

namespace YesSql
{
    public interface ITypeService
    {
        string this[Type t] { get; set; }

        Type this[string s] { get; }
    }
}
