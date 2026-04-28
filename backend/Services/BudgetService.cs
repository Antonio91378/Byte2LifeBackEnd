using Byte2Life.API.Models;
using Byte2Life.API.Services.Budget;

namespace Byte2Life.API.Services
{
    public class BudgetService : IBudgetService
    {
        private readonly IFilamentService _filamentService;

        private sealed class ResolvedBudgetFilament
        {
            public required string FilamentId { get; init; }
            public required Filament Filament { get; init; }
            public required double MassGrams { get; init; }
            public required decimal MaterialCost { get; init; }
        }

        public BudgetService(IFilamentService filamentService)
        {
            _filamentService = filamentService;
        }

        private static void AppendBudgetFilament(
            List<BudgetFilamentRequestItem> target,
            string? filamentId,
            double massGrams)
        {
            var normalizedFilamentId = (filamentId ?? string.Empty).Trim();
            var normalizedMass = Math.Max(massGrams, 0);

            if (string.IsNullOrWhiteSpace(normalizedFilamentId) || normalizedMass <= 0)
            {
                return;
            }

            var existingItem = target.FirstOrDefault(item => item.FilamentId == normalizedFilamentId);
            if (existingItem == null)
            {
                target.Add(new BudgetFilamentRequestItem
                {
                    FilamentId = normalizedFilamentId,
                    MassGrams = normalizedMass
                });
                return;
            }

            existingItem.MassGrams += normalizedMass;
        }

        private static List<BudgetFilamentRequestItem> NormalizeBudgetFilaments(BudgetRequest request)
        {
            var normalizedFilaments = new List<BudgetFilamentRequestItem>();

            if (request.Filaments != null)
            {
                foreach (var filament in request.Filaments)
                {
                    AppendBudgetFilament(normalizedFilaments, filament.FilamentId, filament.MassGrams);
                }
            }

            if (normalizedFilaments.Count == 0)
            {
                AppendBudgetFilament(normalizedFilaments, request.FilamentId, request.MassGrams);
            }

            return normalizedFilaments;
        }

        private async Task<List<ResolvedBudgetFilament>> ResolveBudgetFilamentsAsync(BudgetRequest request)
        {
            var requestedFilaments = NormalizeBudgetFilaments(request);
            if (requestedFilaments.Count == 0)
            {
                throw new ArgumentException("At least one filament with valid mass is required");
            }

            var resolvedFilaments = new List<ResolvedBudgetFilament>();
            foreach (var requestedFilament in requestedFilaments)
            {
                var filament = await _filamentService.GetAsync(requestedFilament.FilamentId);
                if (filament == null)
                {
                    throw new ArgumentException($"Filament not found: {requestedFilament.FilamentId}");
                }

                resolvedFilaments.Add(new ResolvedBudgetFilament
                {
                    FilamentId = requestedFilament.FilamentId,
                    Filament = filament,
                    MassGrams = requestedFilament.MassGrams,
                    MaterialCost = (filament.Price / 1000m) * (decimal)requestedFilament.MassGrams
                });
            }

            return resolvedFilaments;
        }

        public async Task<BudgetResult> CalculateBudgetAsync(BudgetRequest request)
        {
            var resolvedFilaments = await ResolveBudgetFilamentsAsync(request);
            var totalMassGrams = resolvedFilaments.Sum(item => item.MassGrams);
            var materialCost = resolvedFilaments.Sum(item => item.MaterialCost);
            var weightedAveragePricePerKg = totalMassGrams > 0
                ? resolvedFilaments.Sum(item => item.Filament.Price * (decimal)item.MassGrams) / (decimal)totalMassGrams
                : 0m;

            // Estimate Time
            // Rates (g/h): Low=20, Normal=15, High=5, Extreme=1
            double estimatedTime;
            if (request.PrintTimeHours.HasValue && request.PrintTimeHours.Value > 0)
            {
                estimatedTime = request.PrintTimeHours.Value;
            }
            else
            {
                double rate = request.DetailLevel switch
                {
                    DetailLevel.Low => 20.0,
                    DetailLevel.Normal => 15.0,
                    DetailLevel.High => 5.0,
                    DetailLevel.Extreme => 1.0,
                    _ => 15.0
                };
                estimatedTime = totalMassGrams / rate;
            }

            // Calculate Production Costs (Standard 3D Printing Cost Algorithm)
            // Bambu Lab A1 Specifics:
            // Power: ~100W average (0.1kW)
            // Depreciation: ~R$ 0.20/h (Low maintenance consumer machine)
            const decimal ElectricityRate = 0.90m; // R$/kWh (Average Brazil)
            const decimal PrinterPowerKW = 0.100m; // 100W (Bambu A1 Average)
            const decimal MachineHourlyCost = 0.20m; // R$/h (Depreciation only)

            decimal energyCost = (decimal)estimatedTime * PrinterPowerKW * ElectricityRate;
            decimal machineCost = (decimal)estimatedTime * MachineHourlyCost;
            decimal totalProductionCost = materialCost + energyCost + machineCost;

            // Use Builder to calculate final price
            var builder = new PricingBuilder(materialCost)
                .WithBaseMargin(request.DetailLevel)
                .AdjustMarginForFilamentCost(weightedAveragePricePerKg)
                .AdjustMarginForTime(estimatedTime)
                .WithCustomArt(request.HasCustomArt)
                .WithPainting(request.HasPainting)
                .WithVarnish(request.HasVarnish)
                .AdjustMarginForNozzle(request.NozzleDiameter);

            var result = builder.Build(request.DetailLevel, request.HasCustomArt, totalMassGrams, estimatedTime, totalProductionCost, request.NozzleDiameter, request.LayerHeight);

            result.MaterialBreakdown = resolvedFilaments
                .Select(item => new BudgetMaterialBreakdownItem
                {
                    FilamentId = item.FilamentId,
                    FilamentDescription = item.Filament.Description,
                    Color = item.Filament.Color,
                    MassGrams = item.MassGrams,
                    UnitPricePerKg = item.Filament.Price,
                    MaterialCost = item.MaterialCost
                })
                .ToList();
            result.EnergyCost = energyCost;
            result.MachineCost = machineCost;
            result.TotalProductionCost = totalProductionCost;

            return result;
        }
    }
}
