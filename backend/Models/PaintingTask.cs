using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Byte2Life.API.Models
{
    [BsonIgnoreExtraElements]
    public class PaintingTask
    {
        [BsonId]
        public ObjectId? Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime? StartAt { get; set; }
        public double DurationHours { get; set; }
        public ObjectId? ResponsibleId { get; set; }
        public string? ResponsibleName { get; set; }
        public decimal? Value { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
