using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YesSql
{
    public interface IIdAccessor<T>
    {
        T Get(object obj);
        void Set(object obj, T value);
    }
}
