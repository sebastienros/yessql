using System.Text.Json.Serialization;

namespace YesSql.Tests.Models
{
    public class Animal
    {
        public string Name { get; set; }

        [JsonIgnore]
        public string Color { get; set; }
    }
}
