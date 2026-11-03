using YesSql.Commands;

namespace YesSql
{
    /// <summary>
    /// Exposes document command hooks for a session.
    /// </summary>
    public interface IDocumentCommandSession
    {
        /// <summary>
        /// Handles create, update, and delete document commands for the session.
        /// </summary>
        IDocumentCommandHandler DocumentCommandHandler { get; set; }
    }
}
