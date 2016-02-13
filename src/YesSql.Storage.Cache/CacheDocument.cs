namespace YesSql.Storage.Cache
{
    /// <summary>
    /// Represents a document as stored with an <see cref="CacheDocumentStorage"/> instance
    /// </summary>
    public class CacheDocument
    {
        /// <summary>
        /// The unique identifier of the document.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// the serialized JSON content of the document.
        /// </summary>
        public string Content { get; set; }
    }
}
