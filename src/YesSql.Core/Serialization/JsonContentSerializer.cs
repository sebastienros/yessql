using System;
using System.Text.Json;

namespace YesSql.Serialization
{
    public class JsonContentSerializer : IContentSerializer
    {
        private readonly static JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions();

        static JsonContentSerializer()
        {
            _jsonSerializerOptions.Converters.Add(new UtcDateTimeJsonConverter());
        }

        public object Deserialize(string content, Type type)
            => JsonSerializer.Deserialize(content, type, _jsonSerializerOptions);

        public dynamic DeserializeDynamic(string content)
            => JsonSerializer.Deserialize<dynamic>(content, _jsonSerializerOptions);

        public string Serialize(object item)
            => JsonSerializer.Serialize(item, _jsonSerializerOptions);
    }
}
