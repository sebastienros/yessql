using System;

namespace YesSql.Storage
{
    public class DocumentIdentity : IIdentityEntity
    {
        public DocumentIdentity(long id, object entity)
        {
            Id = id;
            Entity = entity;
            EntityType = entity.GetType();
        }

        public DocumentIdentity(long id, Type type)
        {
            Id = id;
            Entity = null;
            EntityType = type;
        }

        public long Id { get; set; }
        public object Entity { get; set; }
        public Type EntityType { get; set; }
    }
}
