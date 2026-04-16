using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Byte2Life.API.Models
{
    [BsonIgnoreExtraElements]
    public class Reminder
    {
        [BsonId]
        public ObjectId? Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsDone { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
