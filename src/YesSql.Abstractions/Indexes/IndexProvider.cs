using System;

namespace YesSql.Indexes
{
    public abstract class IndexProvider<T> : IIndexProvider
    {
        public abstract void Describe(DescribeContext<T> context);

        void IIndexProvider.Describe(IDescriptor context)
        {
            Describe((DescribeContext<T>)context);
        }

        public string CollectionName { get; set; }

        public Type ForType()
        {
            return typeof(T);
        }
    }
}
