namespace YesSql.Core.Indexes
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
    }
}
