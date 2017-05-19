using System;
using System.Data;

namespace YesSql
{
    public interface IConnectionFactory : IDisposable
    {
        IDbConnection CreateConnection();
    }
}
