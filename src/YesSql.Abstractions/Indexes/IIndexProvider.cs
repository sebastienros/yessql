using System;

namespace YesSql.Indexes
{
    /// <summary>
    /// Provides the index descriptions for a given document type so the store can
    /// maintain the corresponding index tables.
    /// </summary>
    public interface IIndexProvider
    {
        /// <summary>
        /// Describes the indexes produced for the document type handled by this provider.
        /// </summary>
        /// <param name="context">The descriptor context to populate.</param>
        void Describe(IDescriptor context);

        /// <summary>
        /// Gets the document type that this provider creates indexes for.
        /// </summary>
        /// <returns>The document type.</returns>
        Type ForType();

        /// <summary>
        /// Gets or sets the name of the collection the provider applies to.
        /// </summary>
        string CollectionName { get; set; }
    }
}