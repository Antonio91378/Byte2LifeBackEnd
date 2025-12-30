namespace Byte2Life.API.Models
{
    public class PrintIncident
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Reason { get; set; } = string.Empty; // PowerLoss, FilamentJam, etc.
        public string Comment { get; set; } = string.Empty;
    }
}
