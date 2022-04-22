using System;

namespace YesSql.Storage
{
    public interface IIdentityEntity
    {
        int Id { get; set; }
        object Entity { get; set; }
        Type EntityType { get; set; }
    }
}
