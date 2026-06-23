using System;

namespace YesSql.Indexes
{
    /// <summary>
    /// A base class for index providers that describe the indexes for documents of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The document type the indexes are produced for.</typeparam>
    public abstract class IndexProvider<T> : IIndexProvider
    {
        /// <summary>
        /// Describes the indexes produced for documents of type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="context">The strongly typed descriptor context to populate.</param>
        public abstract void Describe(DescribeContext<T> context);

        void IIndexProvider.Describe(IDescriptor context)
        {
            Describe((DescribeContext<T>)context);
        }

        /// <summary>
        /// Gets or sets the name of the collection the provider applies to.
        /// </summary>
        public string CollectionName { get; set; }

        /// <summary>
        /// Gets the document type that this provider creates indexes for.
        /// </summary>
        /// <returns>The document type <typeparamref name="T"/>.</returns>
        public Type ForType() => typeof(T);
    }
}
