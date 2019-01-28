using System;
using System.Data.Common;

namespace YesSql
{
    public interface IConnectionFactory
    {
        DbConnection CreateConnection();
        Type DbConnectionType { get; }
    }
}
