using LiteDB;

namespace Byte2Life.API.Models
{
    public class Sale
    {
        public ObjectId? Id { get; set; }

        public string Description { get; set; } = string.Empty;
        public string? ProductLink { get; set; }
        public string? PrintQuality { get; set; }
        public double MassGrams { get; set; }
        public decimal Cost { get; set; }
        public decimal SaleValue { get; set; }
        public decimal Profit { get; set; }
        public string? ProfitPercentage { get; set; }
        public string? DesignPrintTime { get; set; }
        public double PrintTimeHours { get; set; } // Total estimated time in hours
        public DateTime SaleDate { get; set; } = DateTime.Now;
        public DateTime? DeliveryDate { get; set; }
        public DateTime? PrintStartScheduledAt { get; set; }
        public DateTime? PrintStartConfirmedAt { get; set; }

        public string PrintStatus { get; set; } = "Pending"; // Pending, InQueue, Staged, InProgress, Concluded
        public int Priority { get; set; } = 0;
        public DateTime? PrintStartedAt { get; set; }
        public List<string> Tags { get; set; } = new();
        public List<PrintIncident> Incidents { get; set; } = new();
        
        public string? ErrorReason { get; set; }
        public double? WastedFilamentGrams { get; set; }

        public bool IsPrintConcluded { get; set; }
        public bool IsDelivered { get; set; }
        public bool IsPaid { get; set; }

        public ObjectId? FilamentId { get; set; }
        
        public ObjectId? ClientId { get; set; }

        public ObjectId? StockItemId { get; set; }

        public bool HasCustomArt { get; set; }
        public bool HasPainting { get; set; }
        public bool HasVarnish { get; set; }

        public string? NozzleDiameter { get; set; }
        public string? LayerHeight { get; set; }
        public decimal? ProductionCost { get; set; }
    }
}
