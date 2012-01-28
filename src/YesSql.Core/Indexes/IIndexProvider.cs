namespace YesSql.Core.Indexes
{
    public interface IIndexProvider
    {
        void Describe(DescribeContext context);
    }
}