using LiteDB;

namespace Byte2Life.API.Models
{
    public class ServiceProvider
    {
        public ObjectId? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<string> Categories { get; set; } = new();
        public string? Category { get; set; }
    }
}
