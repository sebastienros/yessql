using System;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace YesSql.Serialization
{
    public class UtcDateTimeJsonConverter : JsonConverter<DateTime>
    {
        public static readonly UtcDateTimeJsonConverter Instance = new();

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Debug.Assert(typeToConvert == typeof(DateTime));

            if (!reader.TryGetDateTime(out DateTime value))
            {
                value = DateTime.UtcNow;
            }

            return value;
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToUniversalTime());
        }
    }
}
