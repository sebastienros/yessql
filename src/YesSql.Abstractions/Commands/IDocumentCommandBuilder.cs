namespace YesSql.Commands
{
    public interface IDocumentCommandBuilder
    {
        DocumentCommand BuildCreateDocumentCommand(object entity, Document document, IStore store, string collection);
        DocumentCommand BuildUpdateDocumentCommand(object entity, Document document, IStore store, long checkVersion, string collection);
        DocumentCommand BuildDeleteDocumentCommand(object entity, Document document, IStore store, string collection);
    }
}
