using System;
using System.Text.Json;

namespace YesSql.Serialization
{
    public class JsonContentSerializer : IContentSerializer
    {
        public object Deserialize(string content, Type type)
            => JsonSerializer.Deserialize(content, type);

        public dynamic DeserializeDynamic(string content)
            => JsonSerializer.Deserialize<dynamic>(content);

        public string Serialize(object item)
            => JsonSerializer.Serialize(item);
    }
}
