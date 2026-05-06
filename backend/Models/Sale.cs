using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Byte2Life.API.Models
{
    public class SaleFilamentUsage
    {
        public ObjectId? FilamentId { get; set; }
        public double MassGrams { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class SalePrintFeedbackRating
    {
        public int Stars { get; set; }
        public string? Reason { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class SalePrintFeedback
    {
        public SalePrintFeedbackRating FileQuality { get; set; } = new();
        public SalePrintFeedbackRating PrintQuality { get; set; } = new();
        public string? GeneralNotes { get; set; }
        public DateTime? RecordedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class SalePrintFeedbackHistoryEntry
    {
        public string? SourceSaleId { get; set; }
        public string? SourceSaleDescription { get; set; }
        public DateTime? ClonedAt { get; set; }
        public SalePrintFeedback? Feedback { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class Sale
    {
        [BsonId]
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
        public DateTime? PaintStartConfirmedAt { get; set; }
        public DateTime? DesignStartConfirmedAt { get; set; }

        public string PrintStatus { get; set; } = "Pending"; // Pending, InQueue, Staged, InProgress, Concluded
        public int Priority { get; set; } = 0;
        public DateTime? PrintStartedAt { get; set; }
        public List<string> Tags { get; set; } = new();
        public List<PrintIncident> Incidents { get; set; } = new();
        
        public string? ErrorReason { get; set; }
        public double? WastedFilamentGrams { get; set; }
        public SalePrintFeedback? PrintFeedback { get; set; }
        public List<SalePrintFeedbackHistoryEntry> PrintFeedbackHistory { get; set; } = new();

        public bool IsPrintConcluded { get; set; }
        public bool IsDelivered { get; set; }
        public bool IsPaid { get; set; }
        public bool? IsActive { get; set; }

        public List<SaleFilamentUsage> Filaments { get; set; } = new();
        public ObjectId? FilamentId { get; set; }
        
        public ObjectId? ClientId { get; set; }

        public ObjectId? StockItemId { get; set; }

        public bool HasCustomArt { get; set; }
        public bool HasPainting { get; set; }
        public bool HasVarnish { get; set; }
        public double DesignTimeHours { get; set; }
        public string? DesignResponsible { get; set; }
        public decimal? DesignValue { get; set; }
        public string? DesignStatus { get; set; }
        public double PaintTimeHours { get; set; }
        public string? PaintResponsible { get; set; }
        public string? PaintStatus { get; set; }

        public string? NozzleDiameter { get; set; }
        public string? LayerHeight { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal? ProductionCost { get; set; }
        public List<SaleAttachment> Attachments { get; set; } = new();
    }
}
