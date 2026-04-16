using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Byte2Life.API.Models
{
    [BsonIgnoreExtraElements]
    public class Paint
    {
        [BsonId]
        public ObjectId? Id { get; set; }
        public string Brand { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string Color { get; set; } = string.Empty;
        public string? ColorHex { get; set; }
        public string Type { get; set; } = string.Empty; // Acrylic, Enamel, Primer, Varnish
        public double VolumeMl { get; set; }
        public int StockQuantity { get; set; }
        public bool IsLowStock { get; set; }
    }
}
