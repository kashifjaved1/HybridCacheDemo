using System.Text.Json.Serialization;

namespace SecuringWebApi.Data.Entities
{
    public sealed class Item : BaseEntity
    {
        public string Name { get; set; }
        public string Description { get; set; }

        [JsonIgnore]
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
