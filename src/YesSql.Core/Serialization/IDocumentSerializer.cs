using YesSql.Core.Data.Models;

namespace YesSql.Core.Serialization
{
    public interface IDocumentSerializer
    {
        void Serialize(object obj, ref Document doc);
        object Deserialize(Document doc);
    }
}
