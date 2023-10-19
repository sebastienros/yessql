using System;
using System.Text.Json;

namespace YesSql.Serialization
{
    public class DefaultContentSerializer : IContentSerializer
    {
        private readonly JsonSerializerOptions _options;

        public DefaultContentSerializer()
        {
            _options = new();
        }

        public DefaultContentSerializer(JsonSerializerOptions options)
        {
            _options = options;
        }

        public object Deserialize(string content, Type type)
        {
            return JsonSerializer.Deserialize(content, type);
        }

        public string Serialize(object item)
        {
            return JsonSerializer.Serialize(item);
        }
    }
}
