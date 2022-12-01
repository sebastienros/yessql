namespace YesSql
{
    /// <summary>
    /// The class stored in the Document table of a collection.
    /// </summary>
    public class Document
    {
        /// <summary>
        /// The unique identifier of the document in the database.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// The type of the document.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the serialized content of the document.
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Gets or sets the version number of the document.
        /// </summary>
        /// <remarks>
        /// This property is used to track updates, and optionally detect concurrency violations.
        /// </remarks>
        public long Version { get; set; }

        /// <summary>
        /// Clones the current document.
        /// </summary>
        /// <returns>A clone of the current document.</returns>
        public Document Clone()
        {
            return new Document
            {
                Id = Id,
                Type = Type,
                Content = Content,
                Version = Version
            };
        }
    }
}
