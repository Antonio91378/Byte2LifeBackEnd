using Byte2Life.API.Models;
using Byte2Life.API.Persistence;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Byte2Life.API.Services
{
    public class SaleService : ISaleService
    {
        private readonly IMongoCollection<Sale> _salesCollection;
        private readonly IFilamentService _filamentService;
        private static readonly TimeSpan PrintStartWindow = new(8, 0, 0);
        private static readonly TimeSpan LastPrintStartWindow = new(23, 0, 0);
        private static readonly TimeSpan PrintGap = TimeSpan.FromMinutes(20);

        public SaleService(IMongoDatabase database, IFilamentService filamentService)
        {
            _salesCollection = database.GetCollection<Sale>(MongoCollectionNames.Sales);
            _filamentService = filamentService;
        }

        public Task<List<Sale>> GetAsync(string? dateFilter = null)
        {
            if (string.IsNullOrWhiteSpace(dateFilter))
            {
                return Task.FromResult(NormalizeFinancialFields(_salesCollection.Find(FilterDefinition<Sale>.Empty).ToList()));
            }

            if (dateFilter.Length == 7 && dateFilter.Contains("-"))
            {
                if (DateTime.TryParseExact(dateFilter, "yyyy-MM", null, System.Globalization.DateTimeStyles.None, out var monthDate))
                {
                    var start = new DateTime(monthDate.Year, monthDate.Month, 1);
                    var end = start.AddMonths(1);
                    var filter = Builders<Sale>.Filter.Gte(sale => sale.SaleDate, start)
                        & Builders<Sale>.Filter.Lt(sale => sale.SaleDate, end);
                    return Task.FromResult(NormalizeFinancialFields(_salesCollection.Find(filter).ToList()));
                }
            }

            if (DateTime.TryParse(dateFilter, out var dayDate))
            {
                var start = dayDate.Date;
                var end = start.AddDays(1);
                var filter = Builders<Sale>.Filter.Gte(sale => sale.SaleDate, start)
                    & Builders<Sale>.Filter.Lt(sale => sale.SaleDate, end);
                return Task.FromResult(NormalizeFinancialFields(_salesCollection.Find(filter).ToList()));
            }

            return Task.FromResult(NormalizeFinancialFields(_salesCollection.Find(FilterDefinition<Sale>.Empty).ToList()));
        }

        public Task<Sale?> GetByIdAsync(string id) =>
            Task.FromResult(FindSaleById(id));

        public Task<List<Sale>> GetByFilamentIdAsync(string filamentId)
        {
            var objectId = MongoId.Parse(filamentId);
            return Task.FromResult(NormalizeFinancialFields(_salesCollection.AsQueryable().Where(sale => sale.FilamentId == objectId).ToList()));
        }

        public Task<Sale?> GetCurrentPrintAsync() =>
            Task.FromResult(GetNormalizedCurrentPrint());

        public Task<List<Sale>> GetQueueAsync()
        {
            var queue = _salesCollection.Find(FilterDefinition<Sale>.Empty).ToList()
                .Where(sale => IsSaleActive(sale) && sale.PrintStatus == "InQueue")
                .OrderByDescending(sale => sale.Priority)
                .ToList();

            return Task.FromResult(NormalizeFinancialFields(UpdateQueueSchedule(queue)));
        }

        public Task<List<Sale>> GetPaintingScheduleAsync()
        {
            var sales = _salesCollection.Find(FilterDefinition<Sale>.Empty).ToList();
            return Task.FromResult(NormalizeFinancialFields(sales.Where(sale =>
                IsSaleActive(sale) && (
                    sale.HasPainting ||
                    sale.PaintStartConfirmedAt != null ||
                    sale.PaintTimeHours > 0 ||
                    !string.IsNullOrWhiteSpace(sale.PaintResponsible))).ToList()));
        }

        public Task<List<Sale>> GetServiceScheduleAsync()
        {
            var sales = _salesCollection.Find(FilterDefinition<Sale>.Empty).ToList();
            return Task.FromResult(NormalizeFinancialFields(sales.Where(sale =>
                IsSaleActive(sale) && (
                    sale.HasPainting ||
                    sale.PaintStartConfirmedAt != null ||
                    sale.PaintTimeHours > 0 ||
                    !string.IsNullOrWhiteSpace(sale.PaintResponsible) ||
                    sale.HasCustomArt ||
                    sale.DesignStartConfirmedAt != null ||
                    sale.DesignTimeHours > 0 ||
                    !string.IsNullOrWhiteSpace(sale.DesignResponsible) ||
                    sale.DesignValue.HasValue)).ToList()));
        }

        private static string NormalizeServiceStatus(string? value)
        {
            return string.Equals(value, "Concluded", StringComparison.OrdinalIgnoreCase)
                ? "Concluded"
                : "Active";
        }

        private static bool IsSaleActive(Sale sale) => sale.IsActive != false;

        private static void NormalizeActivityStatus(Sale sale, bool? fallbackValue = true)
        {
            sale.IsActive ??= fallbackValue ?? true;
        }

        private static int ResolvePriority(DateTime? deliveryDate, int requestedPriority, int fallbackPriority)
        {
            if (deliveryDate.HasValue)
            {
                var daysUntilDelivery = (deliveryDate.Value.Date - DateTime.Now.Date).TotalDays;
                if (daysUntilDelivery <= 0) return 0;
                if (daysUntilDelivery <= 1) return 1;
                if (daysUntilDelivery <= 3) return 2;
                if (daysUntilDelivery <= 7) return 3;
                return 5;
            }

            return requestedPriority > 0 ? requestedPriority : fallbackPriority;
        }

        private Sale? FindSaleById(string id)
        {
            var sale = _salesCollection.Find(MongoId.FilterById<Sale>(id)).FirstOrDefault();
            if (sale is null)
            {
                return null;
            }

            return NormalizeFinancialFields(sale);
        }

        private Sale? GetNormalizedCurrentPrint()
        {
            var sale = _salesCollection.Find(FilterDefinition<Sale>.Empty).ToList()
                .FirstOrDefault(currentSale =>
                    IsSaleActive(currentSale) &&
                    (currentSale.PrintStatus == "InProgress" || currentSale.PrintStatus == "Staged"));

            if (sale is null)
            {
                return null;
            }

            return NormalizeFinancialFields(sale);
        }

        private static decimal ResolveBaseCost(Sale sale)
        {
            if (sale.ProductionCost.HasValue && sale.ProductionCost.Value > 0)
            {
                return sale.ProductionCost.Value;
            }

            if (sale.Cost <= 0)
            {
                return 0;
            }

            var shippingCost = Math.Max(sale.ShippingCost, 0);
            var derivedBaseCost = sale.Cost - shippingCost;
            return derivedBaseCost > 0 ? derivedBaseCost : sale.Cost;
        }

        private static double GetTrackedWasteGrams(Sale sale)
        {
            return Math.Max(sale.WastedFilamentGrams ?? 0, 0);
        }

        private static double GetTrackedFilamentUsageGrams(Sale sale)
        {
            return Math.Max(sale.MassGrams, 0) + GetTrackedWasteGrams(sale);
        }

        private static Sale NormalizeFinancialFields(Sale sale)
        {
            NormalizeActivityStatus(sale);

            if (sale.Cost <= 0 && (sale.ProductionCost.GetValueOrDefault() > 0 || sale.ShippingCost > 0))
            {
                sale.Cost = sale.ProductionCost.GetValueOrDefault() + Math.Max(sale.ShippingCost, 0);
            }

            sale.Profit = sale.SaleValue - sale.Cost;

            var baseCost = ResolveBaseCost(sale);
            sale.ProfitPercentage = baseCost > 0
                ? ((sale.Profit / baseCost) * 100m).ToString("0.00", CultureInfo.InvariantCulture) + "%"
                : "0%";

            return sale;
        }

        private static List<Sale> NormalizeFinancialFields(List<Sale> sales)
        {
            sales.ForEach(sale =>
            {
                NormalizeFinancialFields(sale);
            });
            return sales;
        }

        private static void NormalizePaintingFields(Sale sale)
        {
            if (sale.HasPainting)
            {
                return;
            }

            if (sale.PaintStartConfirmedAt != null || sale.PaintTimeHours > 0 || !string.IsNullOrWhiteSpace(sale.PaintResponsible))
            {
                sale.HasPainting = true;
            }
        }

        private static void NormalizeDesignFields(Sale sale)
        {
            if (sale.HasCustomArt)
            {
                return;
            }

            var hasDesignValue = sale.DesignValue.HasValue && sale.DesignValue.Value > 0;
            if (sale.DesignStartConfirmedAt != null || sale.DesignTimeHours > 0 || !string.IsNullOrWhiteSpace(sale.DesignResponsible) || hasDesignValue)
            {
                sale.HasCustomArt = true;
            }
        }

        private static DateTime? GetPrintEnd(Sale sale)
        {
            var start = sale.PrintStartConfirmedAt ?? sale.PrintStartedAt;
            if (!start.HasValue)
            {
                return null;
            }

            var duration = Math.Max(GetSaleDurationHours(sale), 0);
            if (duration <= 0)
            {
                return null;
            }

            return start.Value.AddHours(duration);
        }

        private static DateTime? GetDesignEnd(Sale sale)
        {
            if (!sale.DesignStartConfirmedAt.HasValue)
            {
                return null;
            }

            var duration = Math.Max(sale.DesignTimeHours, 0);
            if (duration <= 0)
            {
                return null;
            }

            return sale.DesignStartConfirmedAt.Value.AddHours(duration);
        }

        private static void ValidatePaintSchedule(Sale sale)
        {
            if (!sale.PaintStartConfirmedAt.HasValue)
            {
                return;
            }

            var printEnd = GetPrintEnd(sale);
            if (printEnd.HasValue && sale.PaintStartConfirmedAt.Value < printEnd.Value)
            {
                throw new InvalidOperationException("Painting must start after print completion.");
            }
        }

        private static void ValidateDesignSchedule(Sale sale)
        {
            var designEnd = GetDesignEnd(sale);
            if (!designEnd.HasValue)
            {
                return;
            }

            var printStart = sale.PrintStartConfirmedAt ?? sale.PrintStartedAt;
            if (printStart.HasValue && designEnd.Value > printStart.Value)
            {
                throw new InvalidOperationException("Design must finish before print start.");
            }
        }

        private static void AdjustPaintScheduleAfterPrintChange(Sale sale)
        {
            if (!sale.PaintStartConfirmedAt.HasValue)
            {
                return;
            }

            var printEnd = GetPrintEnd(sale);
            if (printEnd.HasValue && sale.PaintStartConfirmedAt.Value < printEnd.Value)
            {
                sale.PaintStartConfirmedAt = null;
            }
        }

        public async Task CreateAsync(Sale newSale)
        {
            if (!newSale.Id.HasValue || newSale.Id.Value == ObjectId.Empty)
            {
                newSale.Id = MongoId.New();
            }

            NormalizeActivityStatus(newSale);

            newSale.Priority = ResolvePriority(newSale.DeliveryDate, newSale.Priority, 10);

            if (IsSaleActive(newSale) && (newSale.PrintStatus == "InProgress" || newSale.PrintStatus == "Staged"))
            {
                var currentPrint = _salesCollection.Find(FilterDefinition<Sale>.Empty).ToList()
                    .FirstOrDefault(sale =>
                        IsSaleActive(sale) &&
                        (sale.PrintStatus == "InProgress" || sale.PrintStatus == "Staged"));

                if (currentPrint != null)
                {
                    throw new InvalidOperationException("Only one sale can be InProgress or Staged");
                }

                if (newSale.PrintStatus == "InProgress")
                {
                    newSale.PrintStartedAt = DateTime.Now;
                }
            }

            if (newSale.FilamentId.HasValue)
            {
                var filament = await _filamentService.GetAsync(newSale.FilamentId.Value.ToString());
                if (filament != null)
                {
                    filament.RemainingMassGrams -= GetTrackedFilamentUsageGrams(newSale);
                    await _filamentService.UpdateAsync(filament.Id!.Value.ToString(), filament);
                }
            }

            NormalizePaintingFields(newSale);
            NormalizeDesignFields(newSale);
            newSale.DesignStatus = NormalizeServiceStatus(newSale.DesignStatus);
            newSale.PaintStatus = NormalizeServiceStatus(newSale.PaintStatus);
            NormalizeFinancialFields(newSale);
            ValidateDesignSchedule(newSale);
            ValidatePaintSchedule(newSale);
            _salesCollection.InsertOne(newSale);
        }

        public async Task UpdateAsync(string id, Sale updatedSale)
        {
            var existingSale = _salesCollection.Find(MongoId.FilterById<Sale>(id)).FirstOrDefault();
            if (existingSale is null)
            {
                throw new InvalidOperationException("Sale not found");
            }

            updatedSale.Id = existingSale.Id;
            updatedSale.IsActive ??= existingSale.IsActive ?? true;

            var wasActive = IsSaleActive(existingSale);

            updatedSale.Priority = ResolvePriority(updatedSale.DeliveryDate, updatedSale.Priority, existingSale.Priority > 0 ? existingSale.Priority : 10);

            if (IsSaleActive(updatedSale) && (updatedSale.PrintStatus == "InProgress" || updatedSale.PrintStatus == "Staged"))
            {
                var currentPrint = _salesCollection.Find(FilterDefinition<Sale>.Empty).ToList()
                    .FirstOrDefault(sale =>
                        IsSaleActive(sale) &&
                        (sale.PrintStatus == "InProgress" || sale.PrintStatus == "Staged"));

                if (currentPrint != null && currentPrint.Id?.ToString() != id)
                {
                    throw new InvalidOperationException("Only one sale can be InProgress or Staged");
                }

                if (updatedSale.PrintStatus == "InProgress" && (existingSale.PrintStatus != "InProgress" || !wasActive))
                {
                    updatedSale.PrintStartedAt = DateTime.Now;
                }
            }

            if (string.IsNullOrWhiteSpace(updatedSale.DesignStatus))
            {
                updatedSale.DesignStatus = existingSale.DesignStatus;
            }

            if (string.IsNullOrWhiteSpace(updatedSale.PaintStatus))
            {
                updatedSale.PaintStatus = existingSale.PaintStatus;
            }

            NormalizePaintingFields(updatedSale);
            NormalizeDesignFields(updatedSale);
            updatedSale.DesignStatus = NormalizeServiceStatus(updatedSale.DesignStatus);
            updatedSale.PaintStatus = NormalizeServiceStatus(updatedSale.PaintStatus);
            NormalizeFinancialFields(updatedSale);
            ValidateDesignSchedule(updatedSale);
            ValidatePaintSchedule(updatedSale);
            await AdjustFilamentStockAsync(existingSale, updatedSale);
            _salesCollection.ReplaceOne(MongoId.FilterById<Sale>(id), updatedSale);
        }

        private async Task AdjustFilamentStockAsync(Sale existingSale, Sale updatedSale)
        {
            var oldFilamentId = existingSale.FilamentId?.ToString();
            var newFilamentId = updatedSale.FilamentId?.ToString();
            var oldTrackedUsage = GetTrackedFilamentUsageGrams(existingSale);
            var newTrackedUsage = GetTrackedFilamentUsageGrams(updatedSale);

            if (string.IsNullOrWhiteSpace(oldFilamentId) && string.IsNullOrWhiteSpace(newFilamentId))
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(oldFilamentId) && string.IsNullOrWhiteSpace(newFilamentId))
            {
                var oldFilament = await _filamentService.GetAsync(oldFilamentId);
                if (oldFilament != null)
                {
                    oldFilament.RemainingMassGrams += oldTrackedUsage;
                    await _filamentService.UpdateAsync(oldFilament.Id!.Value.ToString(), oldFilament);
                }
                return;
            }

            if (string.IsNullOrWhiteSpace(oldFilamentId) && !string.IsNullOrWhiteSpace(newFilamentId))
            {
                var newFilament = await _filamentService.GetAsync(newFilamentId);
                if (newFilament != null)
                {
                    newFilament.RemainingMassGrams -= newTrackedUsage;
                    await _filamentService.UpdateAsync(newFilament.Id!.Value.ToString(), newFilament);
                }
                return;
            }

            if (oldFilamentId == newFilamentId)
            {
                var delta = newTrackedUsage - oldTrackedUsage;
                if (Math.Abs(delta) < 0.0001d)
                {
                    return;
                }

                var filament = await _filamentService.GetAsync(oldFilamentId!);
                if (filament != null)
                {
                    filament.RemainingMassGrams -= delta;
                    await _filamentService.UpdateAsync(filament.Id!.Value.ToString(), filament);
                }
                return;
            }

            var oldFilamentSwap = await _filamentService.GetAsync(oldFilamentId!);
            if (oldFilamentSwap != null)
            {
                oldFilamentSwap.RemainingMassGrams += oldTrackedUsage;
                await _filamentService.UpdateAsync(oldFilamentSwap.Id!.Value.ToString(), oldFilamentSwap);
            }

            var newFilamentSwap = await _filamentService.GetAsync(newFilamentId!);
            if (newFilamentSwap != null)
            {
                newFilamentSwap.RemainingMassGrams -= newTrackedUsage;
                await _filamentService.UpdateAsync(newFilamentSwap.Id!.Value.ToString(), newFilamentSwap);
            }
        }

        public Task UpdateScheduleAsync(string id, DateTime? printStartConfirmedAt)
        {
            var sale = _salesCollection.Find(MongoId.FilterById<Sale>(id)).FirstOrDefault();
            if (sale is null)
            {
                throw new InvalidOperationException("Sale not found");
            }

            sale.PrintStartConfirmedAt = printStartConfirmedAt;
            ValidateDesignSchedule(sale);
            AdjustPaintScheduleAfterPrintChange(sale);
            NormalizeFinancialFields(sale);
            _salesCollection.ReplaceOne(MongoId.FilterById<Sale>(id), sale);
            return Task.CompletedTask;
        }

        public Task UpdatePaintScheduleAsync(string id, DateTime? paintStartConfirmedAt, double? paintTimeHours, string? paintResponsible)
        {
            var sale = _salesCollection.Find(MongoId.FilterById<Sale>(id)).FirstOrDefault();
            if (sale is null)
            {
                throw new InvalidOperationException("Sale not found");
            }

            sale.PaintStartConfirmedAt = paintStartConfirmedAt;
            if (paintTimeHours.HasValue)
            {
                sale.PaintTimeHours = Math.Max(0, paintTimeHours.Value);
            }
            if (paintResponsible != null)
            {
                sale.PaintResponsible = paintResponsible.Trim();
            }
            NormalizePaintingFields(sale);
            ValidatePaintSchedule(sale);
            NormalizeFinancialFields(sale);
            _salesCollection.ReplaceOne(MongoId.FilterById<Sale>(id), sale);
            return Task.CompletedTask;
        }

        public Task UpdateDesignScheduleAsync(string id, DateTime? designStartConfirmedAt, double? designTimeHours, string? designResponsible, decimal? designValue)
        {
            var sale = _salesCollection.Find(MongoId.FilterById<Sale>(id)).FirstOrDefault();
            if (sale is null)
            {
                throw new InvalidOperationException("Sale not found");
            }

            sale.DesignStartConfirmedAt = designStartConfirmedAt;
            if (designTimeHours.HasValue)
            {
                sale.DesignTimeHours = Math.Max(0, designTimeHours.Value);
            }
            if (designResponsible != null)
            {
                sale.DesignResponsible = designResponsible.Trim();
            }
            if (designValue.HasValue)
            {
                sale.DesignValue = designValue;
            }

            NormalizeDesignFields(sale);
            ValidateDesignSchedule(sale);
            NormalizeFinancialFields(sale);
            _salesCollection.ReplaceOne(MongoId.FilterById<Sale>(id), sale);
            return Task.CompletedTask;
        }

        public Task UpdateDesignStatusAsync(string id, string? designStatus)
        {
            var sale = _salesCollection.Find(MongoId.FilterById<Sale>(id)).FirstOrDefault();
            if (sale is null)
            {
                throw new InvalidOperationException("Sale not found");
            }

            sale.DesignStatus = NormalizeServiceStatus(designStatus);
            NormalizeFinancialFields(sale);
            _salesCollection.ReplaceOne(MongoId.FilterById<Sale>(id), sale);
            return Task.CompletedTask;
        }

        public Task UpdatePaintStatusAsync(string id, string? paintStatus)
        {
            var sale = _salesCollection.Find(MongoId.FilterById<Sale>(id)).FirstOrDefault();
            if (sale is null)
            {
                throw new InvalidOperationException("Sale not found");
            }

            sale.PaintStatus = NormalizeServiceStatus(paintStatus);
            NormalizeFinancialFields(sale);
            _salesCollection.ReplaceOne(MongoId.FilterById<Sale>(id), sale);
            return Task.CompletedTask;
        }

        public async Task RemoveAsync(string id)
        {
            var sale = await GetByIdAsync(id);
            if (sale != null && sale.FilamentId.HasValue)
            {
                var filament = await _filamentService.GetAsync(sale.FilamentId.Value.ToString());
                if (filament != null)
                {
                    filament.RemainingMassGrams += GetTrackedFilamentUsageGrams(sale);
                    await _filamentService.UpdateAsync(filament.Id!.Value.ToString(), filament);
                }
            }

            _salesCollection.DeleteOne(MongoId.FilterById<Sale>(id));
        }

        private static DateTime AlignStart(DateTime candidate, bool applyGapIfShifted)
        {
            var dayStart = candidate.Date.Add(PrintStartWindow);
            var dayEndStart = candidate.Date.Add(LastPrintStartWindow);

            if (candidate < dayStart)
            {
                return applyGapIfShifted ? dayStart.Add(PrintGap) : dayStart;
            }

            if (candidate > dayEndStart)
            {
                var nextDayStart = dayStart.AddDays(1);
                return applyGapIfShifted ? nextDayStart.Add(PrintGap) : nextDayStart;
            }

            return candidate;
        }

        private static DateTime CalculateNextStart(DateTime? previousEnd, DateTime baseTime)
        {
            if (!previousEnd.HasValue)
            {
                return AlignStart(baseTime, false);
            }

            var candidate = previousEnd.Value.Add(PrintGap);
            return AlignStart(candidate, true);
        }

        private static DateTime FindNextAvailableStart(DateTime baseTime, List<(DateTime Start, DateTime End)> occupied, double durationHours)
        {
            var candidate = AlignStart(baseTime, false);
            if (durationHours <= 0)
            {
                return candidate;
            }

            var sorted = occupied.OrderBy(slot => slot.Start).ToList();
            foreach (var slot in sorted)
            {
                var latestStart = slot.Start.Subtract(PrintGap);
                var candidateEnd = candidate.AddHours(durationHours);
                if (candidateEnd <= latestStart)
                {
                    return candidate;
                }

                var blockedEnd = slot.End.Add(PrintGap);
                if (candidate < blockedEnd)
                {
                    candidate = AlignStart(blockedEnd, true);
                }
            }

            return candidate;
        }

        private static double GetSaleDurationHours(Sale sale)
        {
            if (sale.PrintTimeHours > 0)
            {
                return sale.PrintTimeHours;
            }

            if (string.IsNullOrWhiteSpace(sale.DesignPrintTime))
            {
                return 0;
            }

            var text = sale.DesignPrintTime.Trim().ToLowerInvariant();

            if (TimeSpan.TryParse(text, out var parsed))
            {
                return parsed.TotalHours;
            }

            double hours = 0;
            double minutes = 0;

            var hoursMatch = Regex.Match(text, @"(\d+(?:[.,]\d+)?)\s*h", RegexOptions.IgnoreCase);
            if (hoursMatch.Success)
            {
                var value = hoursMatch.Groups[1].Value.Replace(',', '.');
                if (double.TryParse(value, out var parsedHours))
                {
                    hours = parsedHours;
                }
            }

            var minutesMatch = Regex.Match(text, @"(\d+(?:[.,]\d+)?)\s*m", RegexOptions.IgnoreCase);
            if (minutesMatch.Success)
            {
                var value = minutesMatch.Groups[1].Value.Replace(',', '.');
                if (double.TryParse(value, out var parsedMinutes))
                {
                    minutes = parsedMinutes;
                }
            }

            if (hours > 0 || minutes > 0)
            {
                return hours + (minutes / 60d);
            }

            var cleaned = text.Replace(',', '.');
            if (double.TryParse(cleaned, out var fallback))
            {
                return fallback;
            }

            return 0;
        }

        private List<Sale> UpdateQueueSchedule(List<Sale> queue)
        {
            if (queue.Count == 0)
            {
                return queue;
            }

            var baseTime = DateTime.Now;
            var occupied = new List<(DateTime Start, DateTime End)>();
            var currentPrint = _salesCollection.Find(FilterDefinition<Sale>.Empty).ToList()
                .FirstOrDefault(sale =>
                    IsSaleActive(sale) &&
                    (sale.PrintStatus == "InProgress" || sale.PrintStatus == "Staged"));

            if (currentPrint?.PrintStartedAt != null)
            {
                var currentDuration = Math.Max(GetSaleDurationHours(currentPrint), 0);
                if (currentDuration > 0)
                {
                    var start = currentPrint.PrintStartedAt.Value;
                    var end = start.AddHours(currentDuration);
                    occupied.Add((start, end));
                }
            }

            foreach (var sale in queue.Where(item => item.PrintStartConfirmedAt.HasValue))
            {
                var duration = Math.Max(GetSaleDurationHours(sale), 0);
                if (duration <= 0)
                {
                    continue;
                }

                var start = sale.PrintStartConfirmedAt!.Value;
                var end = start.AddHours(duration);
                occupied.Add((start, end));
            }

            foreach (var sale in queue.Where(item => !item.PrintStartConfirmedAt.HasValue))
            {
                var duration = Math.Max(GetSaleDurationHours(sale), 0);
                if (duration <= 0)
                {
                    if (sale.PrintStartScheduledAt != null)
                    {
                        sale.PrintStartScheduledAt = null;
                        _salesCollection.ReplaceOne(MongoId.FilterById<Sale>(sale.Id!.Value), sale);
                    }
                    continue;
                }

                var start = FindNextAvailableStart(baseTime, occupied, duration);
                if (sale.PrintStartScheduledAt != start)
                {
                    sale.PrintStartScheduledAt = start;
                    _salesCollection.ReplaceOne(MongoId.FilterById<Sale>(sale.Id!.Value), sale);
                }
                var end = start.AddHours(duration);
                occupied.Add((start, end));
            }

            return queue;
        }
    }
}
