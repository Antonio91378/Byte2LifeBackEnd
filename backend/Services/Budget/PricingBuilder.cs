using Byte2Life.API.Models;
using System.Text;

namespace Byte2Life.API.Services.Budget
{
    public class PricingBuilder
    {
        private readonly decimal _materialCost;
        private decimal _profitMarginPercentage;
        private readonly StringBuilder _breakdown;

        public PricingBuilder(decimal materialCost)
        {
            _materialCost = materialCost;
            _profitMarginPercentage = 0;
            _breakdown = new StringBuilder();
            _breakdown.AppendLine($"Custo Material: {_materialCost:C}");
        }

        public PricingBuilder WithBaseMargin(DetailLevel level)
        {
            decimal margin = level switch
            {
                DetailLevel.Low => 80m,
                DetailLevel.Normal => 120m,
                DetailLevel.High => 160m,
                DetailLevel.Extreme => 210m,
                _ => 100m
            };
            
            _profitMarginPercentage = margin;
            _breakdown.AppendLine($"Margem Base ({level}): {margin}%");
            return this;
        }

        public PricingBuilder AdjustMarginForFilamentCost(decimal filamentPricePerKg)
        {
            const decimal BaseFilamentPrice = 120m;
            
            if (filamentPricePerKg > BaseFilamentPrice)
            {
                // "se o filamento custar 150 reais por exemplo, esses mesmos 80% serão incrementados de forma proporcional."
                // Interpretation: NewMargin = OldMargin * (Price / BasePrice)
                // Example: 80% * (150 / 120) = 80% * 1.25 = 100%
                
                var ratio = filamentPricePerKg / BaseFilamentPrice;
                var oldMargin = _profitMarginPercentage;
                _profitMarginPercentage *= ratio;
                
                _breakdown.AppendLine($"Ajuste Custo Filamento ({filamentPricePerKg:C}/kg): {oldMargin:F0}% -> {_profitMarginPercentage:F0}%");
            }
            return this;
        }

        public PricingBuilder AdjustMarginForTime(double hours)
        {
            const double BaseTimeThreshold = 8.0;
            const decimal PenaltyPerExtraHour = 5.0m; // 5% per extra hour

            if (hours > BaseTimeThreshold)
            {
                var extraHours = (decimal)(hours - BaseTimeThreshold);
                var penalty = extraHours * PenaltyPerExtraHour;
                _profitMarginPercentage += penalty;
                
                _breakdown.AppendLine($"Ajuste Tempo ({hours}h > {BaseTimeThreshold}h): +{penalty:F0}%");
            }
            return this;
        }

        public PricingBuilder WithCustomArt(bool hasCustomArt)
        {
            if (hasCustomArt)
            {
                const decimal CustomArtMargin = 1200m; // 1200%
                _profitMarginPercentage += CustomArtMargin;
                _breakdown.AppendLine($"Arte Personalizada: +{CustomArtMargin}%");
            }
            return this;
        }

        public PricingBuilder WithPainting(bool hasPainting)
        {
            if (hasPainting)
            {
                const decimal PaintingMargin = 50m; // 50%
                _profitMarginPercentage += PaintingMargin;
                _breakdown.AppendLine($"Pintura: +{PaintingMargin}%");
            }
            return this;
        }

        public PricingBuilder WithVarnish(bool hasVarnish)
        {
            if (hasVarnish)
            {
                const decimal VarnishMargin = 30m; // 30%
                _profitMarginPercentage += VarnishMargin;
                _breakdown.AppendLine($"Verniz: +{VarnishMargin}%");
            }
            return this;
        }

        public PricingBuilder AdjustMarginForNozzle(string? nozzle)
        {
            if (!string.IsNullOrEmpty(nozzle) && nozzle.Contains("0.2"))
            {
                const decimal SmallNozzleMargin = 50m;
                _profitMarginPercentage += SmallNozzleMargin;
                _breakdown.AppendLine($"Ajuste Nozzle 0.2mm: +{SmallNozzleMargin}%");
            }
            return this;
        }

        public BudgetResult Build(DetailLevel level, bool hasCustomArt, double massGrams, double estimatedTime, decimal totalProductionCost, string? nozzleOverride = null, string? layerOverride = null)
        {
            var profitValue = _materialCost * (_profitMarginPercentage / 100m);
            var totalPrice = _materialCost + profitValue;

            // Ensure Price covers Production Cost + Minimum Margin (e.g. 20%)
            // "O valor sugerido nunca deve ser menor que o custo"
            var minPriceByCost = totalProductionCost * 1.20m; // 20% markup on total cost
            
            if (totalPrice < minPriceByCost)
            {
                _breakdown.AppendLine($"Ajuste Custo Produção: Valor calculado ({totalPrice:C}) menor que Custo + 20% ({minPriceByCost:C}). Ajustando.");
                totalPrice = minPriceByCost;
                
                // Recalculate profit based on Material Cost (standardizing the view)
                profitValue = totalPrice - _materialCost;
                _profitMarginPercentage = _materialCost > 0 ? (profitValue / _materialCost) * 100m : 0;
            }

            // Minimum Price Logic for Custom Art
            if (hasCustomArt)
            {
                decimal minPrice = massGrams > 120 ? 100m : 50m;
                if (totalPrice < minPrice)
                {
                    _breakdown.AppendLine($"Ajuste Preço Mínimo (Arte Personalizada): {totalPrice:C} -> {minPrice:C}");
                    totalPrice = minPrice;
                    // Recalculate profit to match total price
                    profitValue = totalPrice - _materialCost;
                    _profitMarginPercentage = _materialCost > 0 ? (profitValue / _materialCost) * 100m : 0;
                }
            }

            (string nozzle, string layer) = level switch
            {
                DetailLevel.Low => ("0.4mm", "0.24mm"),
                DetailLevel.Normal => ("0.4mm", "0.20mm"),
                DetailLevel.High => ("0.4mm", "0.12mm"),
                DetailLevel.Extreme => ("0.2mm", "0.06mm - 0.1mm"),
                _ => ("0.4mm", "0.20mm")
            };

            if (!string.IsNullOrEmpty(nozzleOverride)) nozzle = nozzleOverride;
            if (!string.IsNullOrEmpty(layerOverride)) layer = layerOverride;

            _breakdown.AppendLine($"--------------------------------");
            _breakdown.AppendLine($"Margem Final Total: {_profitMarginPercentage:F0}%");

            return new BudgetResult
            {
                MaterialCost = _materialCost,
                ProfitMarginPercentage = _profitMarginPercentage,
                ProfitValue = profitValue,
                TotalPrice = totalPrice,
                Breakdown = _breakdown.ToString(),
                NozzleDiameter = nozzle,
                LayerHeightRange = layer,
                EstimatedTimeHours = estimatedTime
            };
        }
    }
}
