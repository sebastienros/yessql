using System;
using System.Collections.Generic;

namespace YesSql.Indexes
{
    /// <summary>
    /// Describes the indexes that an <see cref="IIndexProvider"/> produces for a document type.
    /// </summary>
    public interface IDescriptor
    {
        /// <summary>
        /// Builds the <see cref="IndexDescriptor"/> instances for the described indexes.
        /// </summary>
        /// <param name="types">The index types to describe, or an empty array to describe all of them.</param>
        /// <returns>The descriptors matching the requested index types.</returns>
        IEnumerable<IndexDescriptor> Describe(params Type[] types);
    }
}