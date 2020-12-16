using System;

namespace YesSql.Tests.Models
{
    public class TenantLocation
    {
        public string Name { get; set; }
        public Guid TenantId { get; set; }
        public bool IsRunning { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressCity { get; set; }

    }
}
