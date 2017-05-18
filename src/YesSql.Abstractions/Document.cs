namespace YesSql
{
    public class Document
    {
        /// <summary>
        /// The unique identifier of the document in the database.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The type of the document.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the serialized content of the document.
        /// </summary>
        public string Content { get; set; }
    }
}
