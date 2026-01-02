using LiteDB;
using System.Text.Json.Serialization;

namespace Byte2Life.API.Models
{
    public class Filament
    {
        public ObjectId? Id { get; set; }

        public string Description { get; set; } = string.Empty;
        public string Link { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public double InitialMassGrams { get; set; }
        public double RemainingMassGrams { get; set; }
        public string Color { get; set; } = string.Empty;
        public string? ColorHex { get; set; }

        public string Type { get; set; } = string.Empty; // PLA, PETG, ABS, etc.
        [JsonPropertyName("warningComment")]
        public string WarningComment { get; set; } = string.Empty;

        [JsonPropertyName("slicingProfile3mfPath")]
        public string SlicingProfile3mfPath { get; set; } = string.Empty;
    }
}
