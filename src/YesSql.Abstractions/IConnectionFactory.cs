using System;
using System.Data;

namespace YesSql
{
    public interface IConnectionFactory : IDisposable
    {
        IDbConnection CreateConnection();

        /// <summary>
        /// <c>true</c> if the created connection can be disposed by the client.
        /// </summary>
        bool Disposable { get; }
    }
}
