using System;

namespace YesSql.Storage
{
    /// <summary>
    /// Represents an entity tracked by the storage layer together with its identifier and type.
    /// </summary>
    public interface IIdentityEntity
    {
        /// <summary>
        /// Gets or sets the unique identifier of the document.
        /// </summary>
        long Id { get; set; }

        /// <summary>
        /// Gets or sets the entity instance.
        /// </summary>
        object Entity { get; set; }

        /// <summary>
        /// Gets or sets the type of the entity.
        /// </summary>
        Type EntityType { get; set; }
    }
}
