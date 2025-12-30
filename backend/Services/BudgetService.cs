using Byte2Life.API.Models;
using Byte2Life.API.Services.Budget;

namespace Byte2Life.API.Services
{
    public class BudgetService : IBudgetService
    {
        private readonly IFilamentService _filamentService;

        public BudgetService(IFilamentService filamentService)
        {
            _filamentService = filamentService;
        }

        public async Task<BudgetResult> CalculateBudgetAsync(BudgetRequest request)
        {
            var filament = await _filamentService.GetAsync(request.FilamentId);
            if (filament == null)
            {
                throw new ArgumentException("Filament not found");
            }

            // Calculate Material Cost
            // Price is per Kg (1000g)
            // Cost = (Price / 1000) * Mass
            decimal materialCost = (filament.Price / 1000m) * (decimal)request.MassGrams;

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
                estimatedTime = request.MassGrams / rate;
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
                .AdjustMarginForFilamentCost(filament.Price)
                .AdjustMarginForTime(estimatedTime)
                .WithCustomArt(request.HasCustomArt)
                .WithPainting(request.HasPainting)
                .WithVarnish(request.HasVarnish)
                .AdjustMarginForNozzle(request.NozzleDiameter);

            var result = builder.Build(request.DetailLevel, request.HasCustomArt, request.MassGrams, estimatedTime, totalProductionCost, request.NozzleDiameter, request.LayerHeight);

            result.EnergyCost = energyCost;
            result.MachineCost = machineCost;
            result.TotalProductionCost = totalProductionCost;

            return result;
        }
    }
}
