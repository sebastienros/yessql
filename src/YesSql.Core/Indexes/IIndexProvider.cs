using System;

namespace YesSql.Core.Indexes
{
    public interface IIndexProvider
    {
        void Describe(IDescriptor context);
        Type ForType();
    }

    public abstract class IndexProvider<T> : IIndexProvider
    {
        public abstract void Describe(DescribeContext<T> context);

        void IIndexProvider.Describe(IDescriptor context)
        {
            Describe((DescribeContext<T>)context);
        }

        public Type ForType()
        {
            return typeof(T);
        }
    }
}