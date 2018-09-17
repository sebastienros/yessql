using System;
using System.Linq.Expressions;

namespace YesSql
{
    public interface ICompiledQuery<T> where T : class
    {
        Expression<Func<IQuery, IQuery<T>>> Query();
    }
}
