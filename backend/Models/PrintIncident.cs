using MongoDB.Bson;

namespace Byte2Life.API.Models
{
    public class PrintIncidentFilamentWaste
    {
        public ObjectId? FilamentId { get; set; }
        public double MassGrams { get; set; }
    }

    public class PrintIncident
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Reason { get; set; } = string.Empty; // PowerLoss, FilamentJam, etc.
        public string Comment { get; set; } = string.Empty;
        public double? WastedFilamentGrams { get; set; }
        public List<PrintIncidentFilamentWaste> WastedFilaments { get; set; } = new();
    }
}
