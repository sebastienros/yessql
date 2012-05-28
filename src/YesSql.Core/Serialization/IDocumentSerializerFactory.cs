using YesSql.Core.Services;

namespace YesSql.Core.Serialization
{
    public interface IDocumentSerializerFactory
    {
        IDocumentSerializer Build(IStore store);
    }
}
