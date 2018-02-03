using Newtonsoft.Json;
using System;

namespace YesSql.Serialization
{
    public class JsonContentSerializer : IContentSerializer
    {
        private readonly static JsonSerializerSettings _jsonSettings = new JsonSerializerSettings {
            TypeNameHandling = TypeNameHandling.Auto,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc
        };

        public object Deserialize(string content, Type type)
        {
            return JsonConvert.DeserializeObject(content, type, _jsonSettings);
        }

        public dynamic DeserializeDynamic(string content)
        {
            return JsonConvert.DeserializeObject<dynamic>(content, _jsonSettings);
        }

        public string Serialize(object item)
        {
            return JsonConvert.SerializeObject(item, _jsonSettings);
        }
    }
}
