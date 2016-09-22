using System;

namespace YesSql.Core.Indexes
{
    public interface IIndexProvider
    {
        void Describe(IDescriptor context);
        Type ForType();
        string CollectionName { get; set; }
    }

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