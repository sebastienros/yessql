using System.Text.Json.Serialization;

namespace YesSql.Entites
{
    public interface IVersionable
    {
        [JsonIgnore]
        long Version { get; set; }
    }
}
