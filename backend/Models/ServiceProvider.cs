using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Byte2Life.API.Models
{
    [BsonIgnoreExtraElements]
    public class ServiceProvider
    {
        [BsonId]
        public ObjectId? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        public List<string> Categories { get; set; } = new();
        public string? Category { get; set; }
    }
}
