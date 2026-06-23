using System;

namespace YesSql.Storage
{
    /// <summary>
    /// Associates an entity with its document identifier and type.
    /// </summary>
    public class DocumentIdentity : IIdentityEntity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentIdentity"/> class for an existing entity.
        /// </summary>
        /// <param name="id">The unique identifier of the document.</param>
        /// <param name="entity">The entity instance.</param>
        public DocumentIdentity(long id, object entity)
        {
            Id = id;
            Entity = entity;
            EntityType = entity.GetType();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentIdentity"/> class for an entity that has not been loaded yet.
        /// </summary>
        /// <param name="id">The unique identifier of the document.</param>
        /// <param name="type">The type of the entity.</param>
        public DocumentIdentity(long id, Type type)
        {
            Id = id;
            Entity = null;
            EntityType = type;
        }

        /// <summary>
        /// Gets or sets the unique identifier of the document.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the entity instance, or <c>null</c> when it has not been loaded.
        /// </summary>
        public object Entity { get; set; }

        /// <summary>
        /// Gets or sets the type of the entity.
        /// </summary>
        public Type EntityType { get; set; }
    }
}
