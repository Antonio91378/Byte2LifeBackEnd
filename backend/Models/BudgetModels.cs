namespace Byte2Life.API.Models
{
    public enum DetailLevel
    {
        Low,
        Normal,
        High,
        Extreme
    }

    public class BudgetRequest
    {
        public List<BudgetFilamentRequestItem> Filaments { get; set; } = new();
        public string FilamentId { get; set; } = string.Empty;
        public DetailLevel DetailLevel { get; set; }
        public double MassGrams { get; set; }
        public bool HasCustomArt { get; set; }
        public bool HasPainting { get; set; }
        public bool HasVarnish { get; set; }
        public double? PrintTimeHours { get; set; }
        public string? NozzleDiameter { get; set; }
        public string? LayerHeight { get; set; }
    }

    public class BudgetFilamentRequestItem
    {
        public string FilamentId { get; set; } = string.Empty;
        public double MassGrams { get; set; }
    }

    public class BudgetMaterialBreakdownItem
    {
        public string FilamentId { get; set; } = string.Empty;
        public string FilamentDescription { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public double MassGrams { get; set; }
        public decimal UnitPricePerKg { get; set; }
        public decimal MaterialCost { get; set; }
    }

    public class BudgetResult
    {
        public decimal MaterialCost { get; set; }
        public List<BudgetMaterialBreakdownItem> MaterialBreakdown { get; set; } = new();
        public decimal EnergyCost { get; set; }
        public decimal MachineCost { get; set; }
        public decimal TotalProductionCost { get; set; }
        public decimal ProfitMarginPercentage { get; set; }
        public decimal ProfitValue { get; set; }
        public decimal TotalPrice { get; set; }
        public string Breakdown { get; set; } = string.Empty;
        public string NozzleDiameter { get; set; } = string.Empty;
        public string LayerHeightRange { get; set; } = string.Empty;
        public double EstimatedTimeHours { get; set; }
    }
}
