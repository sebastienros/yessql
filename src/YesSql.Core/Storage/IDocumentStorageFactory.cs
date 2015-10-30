namespace YesSql.Core.Storage
{
    public interface IDocumentStorageFactory
    {
        IDocumentStorage CreateDocumentStorage();  
    }
}