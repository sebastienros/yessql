namespace YesSql.Core.Serialization
{
    public interface IDocumentSerializerFactory
    {
        IDocumentSerializer Build();
    }
}
