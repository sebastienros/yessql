using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace YesSql.Serialization
{
    public class DefaultContentSerializer : IContentSerializer
    {
        private readonly JsonSerializerOptions _options;

        public DefaultContentSerializer()
        {
            _options = new()
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            _options.Converters.Add(UtcDateTimeJsonConverter.Instance);
            _options.Converters.Add(DynamicJsonConverter.Instance);
        }

        public DefaultContentSerializer(JsonSerializerOptions options) => _options = options;

        public object Deserialize(string content, Type type) => JsonSerializer.Deserialize(content, type, _options);

        public string Serialize(object item) => JsonSerializer.Serialize(item, _options);
    }
}
