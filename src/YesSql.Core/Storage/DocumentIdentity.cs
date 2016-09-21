using System;

namespace YesSql.Core.Storage
{
    public interface IIdentityEntity
    {
        int Id { get; set; }
        object Entity { get; set; }
        Type EntityType { get; set; }
    }

    public class IdentityDocument : IIdentityEntity
    {
        public IdentityDocument(int id, object entity)
        {
            Id = id;
            Entity = entity;
            EntityType = entity.GetType();
        }

        public IdentityDocument(int id, Type type)
        {
            Id = id;
            Entity = null;
            EntityType = type;
        }

        public int Id { get; set; }
        public object Entity { get; set; }
        public Type EntityType { get; set; }
    }
}
