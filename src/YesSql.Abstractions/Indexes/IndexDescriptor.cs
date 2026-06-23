using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace YesSql.Indexes
{
    /// <summary>
    /// Holds the delegates and metadata that define how an index is mapped, grouped,
    /// reduced, updated and deleted for a document type.
    /// </summary>
    public class IndexDescriptor
    {
        /// <summary>
        /// Gets or sets the document type the index is built from.
        /// </summary>
        public Type Type { get; set; }

        /// <summary>
        /// Gets or sets the delegate that maps a document to a set of indexes.
        /// </summary>
        public Func<object, CancellationToken, Task<IEnumerable<IIndex>>> Map { get; set; }

        /// <summary>
        /// Gets or sets the delegate that reduces a group of indexes into a single index.
        /// </summary>
        public Func<IGrouping<object, IIndex>, IIndex> Reduce { get; set; }

        /// <summary>
        /// Gets or sets the delegate that updates a reduced index when documents are added.
        /// </summary>
        public Func<IIndex, IEnumerable<IIndex>, IIndex> Update { get; set; }

        /// <summary>
        /// Gets or sets the delegate that updates a reduced index when documents are removed.
        /// </summary>
        public Func<IIndex, IEnumerable<IIndex>, IIndex> Delete { get; set; }

        /// <summary>
        /// Gets or sets the property used to group indexes when reducing.
        /// </summary>
        public PropertyInfo GroupKey { get; set; }

        /// <summary>
        /// Gets or sets the type of the index that is described.
        /// </summary>
        public Type IndexType { get; set; }

        /// <summary>
        /// Gets or sets the predicate used to filter the documents that are mapped.
        /// </summary>
        public Func<object, bool> Filter { get; set; }
    }
}