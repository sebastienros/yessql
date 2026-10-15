using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YesSql.Indexes;

namespace YesSql
{
    /// <summary>
    /// Exposes additional index descriptors for a session.
    /// </summary>
    public interface ISessionExtensionProvider
    {
        /// <summary>
        /// Additional descriptors applied to all mapped types in the session.
        /// </summary>
        IEnumerable<IndexDescriptor> ExtraIndexDescriptors { get; set; }

        /// <summary>
        /// Builds additional descriptors for a given mapped type and collection.
        /// </summary>
        Func<Type, string, Task<IEnumerable<IndexDescriptor>>> BuildExtraIndexDescriptors { get; set; }
    }
}
