using System;
using System.Data.Common;

namespace YesSql
{
    /// <summary>
    /// Represent a component capable of creating <see cref="DbConnection" /> instances.
    /// </summary>
    public interface IConnectionFactory
    {
        /// <summary>
        /// Creates a <see cref="DbConnection" /> instance.
        /// </summary>
        DbConnection CreateConnection();

        /// <summary>
        /// Gets the type of the connection is can create.
        /// </summary>
        Type DbConnectionType { get; }
    }
}
