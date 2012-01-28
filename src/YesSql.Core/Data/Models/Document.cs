namespace YesSql.Core.Data.Models
{
    /// <summary>
    /// This record represents the storage for a Document
    /// </summary>
    public class Document
    {
        /// <summary>
        /// The unique identifier of the document.
        /// </summary>
        public virtual int Id { get; set; }

        /// <summary>
        /// The type of the document.
        /// </summary>
        public virtual string Type { get; set; }

        /// <summary>
        /// the serialized JSON content of the document.
        /// </summary>
        public virtual string Content { get; set; }
    }
}
