using LiteDB;

namespace Byte2Life.API.Models
{
    public class Client
    {
        public ObjectId? Id { get; set; }

        public string PhoneNumber { get; set; } = string.Empty;
        public string Sex { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
