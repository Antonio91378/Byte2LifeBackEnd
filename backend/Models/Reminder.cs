using LiteDB;

namespace Byte2Life.API.Models
{
    public class Reminder
    {
        public ObjectId? Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsDone { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
