using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace YesSql.Serialization
{
    internal class UtcDateTimeJsonConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => DateTime.SpecifyKind(DateTime.Parse(reader.GetString()), DateTimeKind.Utc); // I'm not sure why ToUniversalTime() has different time zone

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToUniversalTime().ToString());
        }
    }
}
