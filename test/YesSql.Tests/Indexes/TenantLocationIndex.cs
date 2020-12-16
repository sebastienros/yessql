using System;
using YesSql.Indexes;
using YesSql.Tests.Models;

namespace YesSql.Tests.Indexes
{
    public class TenantLocationIndex : MapIndex
    {
        public string Name { get; set; }
        public Guid TenantId { get; set; }
        public bool IsRunning { get; set; }
    }

    public class TenantLocationIndexProvider : IndexProvider<TenantLocation>
    {
        public override void Describe(DescribeContext<TenantLocation> context)
        {
            context
                .For<TenantLocationIndex>()
                .Map(property => new TenantLocationIndex
                {
                    Name = property.Name,
                    IsRunning = property.IsRunning,
                    TenantId = property.TenantId,
                });
        }
    }
}
