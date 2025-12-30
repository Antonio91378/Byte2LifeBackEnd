using LiteDB;

namespace Byte2Life.API.Models
{
    public class StockItem
    {
        [BsonId]
        public ObjectId? Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public string FilamentId { get; set; } = string.Empty;
        public string PrintTime { get; set; } = string.Empty;
        public double WeightGrams { get; set; }
        public double Cost { get; set; } // Sale Price
        public double ProductionCost { get; set; } // Actual Production Cost
        public string PrintQuality { get; set; } = "Normal";
        public string NozzleDiameter { get; set; } = "0.4mm";
        public string LayerHeight { get; set; } = "0.2mm";
        public bool HasCustomArt { get; set; }
        public bool HasPainting { get; set; }
        public bool HasVarnish { get; set; }
        public List<string> Photos { get; set; } = new List<string>();
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string Status { get; set; } = "Available"; // Available, Sold
    }
}
