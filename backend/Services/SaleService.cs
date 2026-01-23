using Byte2Life.API.Models;
using LiteDB;
using System;
using System.Text.RegularExpressions;

namespace Byte2Life.API.Services
{
    public class SaleService : ISaleService
    {
        private readonly LiteDatabase _database;
        private readonly ILiteCollection<Sale> _salesCollection;
        private readonly IFilamentService _filamentService;
        private static readonly TimeSpan PrintStartWindow = new(8, 0, 0);
        private static readonly TimeSpan LastPrintStartWindow = new(23, 0, 0);
        private static readonly TimeSpan PrintGap = TimeSpan.FromMinutes(20);

        public SaleService(LiteDatabase database, IFilamentService filamentService)
        {
            _database = database;
            _salesCollection = _database.GetCollection<Sale>("Sales");
            _filamentService = filamentService;
        }

        public Task<List<Sale>> GetAsync(string? dateFilter = null)
        {
            if (string.IsNullOrWhiteSpace(dateFilter))
            {
                return Task.FromResult(_salesCollection.FindAll().ToList());
            }

            // Filter by Month (YYYY-MM)
            if (dateFilter.Length == 7 && dateFilter.Contains("-"))
            {
                if (DateTime.TryParseExact(dateFilter, "yyyy-MM", null, System.Globalization.DateTimeStyles.None, out var monthDate))
                {
                    var start = new DateTime(monthDate.Year, monthDate.Month, 1);
                    var end = start.AddMonths(1);
                    return Task.FromResult(_salesCollection.Find(s => s.SaleDate >= start && s.SaleDate < end).ToList());
                }
            }
            
            // Filter by Day (YYYY-MM-DD)
            if (DateTime.TryParse(dateFilter, out var dayDate))
            {
                var start = dayDate.Date;
                var end = start.AddDays(1);
                return Task.FromResult(_salesCollection.Find(s => s.SaleDate >= start && s.SaleDate < end).ToList());
            }

            return Task.FromResult(_salesCollection.FindAll().ToList());
        }

        public Task<Sale?> GetByIdAsync(string id) =>
            Task.FromResult<Sale?>(_salesCollection.FindById(new ObjectId(id)));

        public Task<List<Sale>> GetByFilamentIdAsync(string filamentId) =>
            Task.FromResult(_salesCollection.Find(s => s.FilamentId == new ObjectId(filamentId)).ToList());

        public Task<Sale?> GetCurrentPrintAsync() =>
            Task.FromResult<Sale?>(_salesCollection.FindOne(s => s.PrintStatus == "InProgress" || s.PrintStatus == "Staged"));

        public Task<List<Sale>> GetQueueAsync() =>
            Task.FromResult(UpdateQueueSchedule(_salesCollection.Find(s => s.PrintStatus == "InQueue")
                .OrderBy(s => s.Priority)
                .ToList()));

        public Task<List<Sale>> GetPaintingScheduleAsync() =>
            Task.FromResult(_salesCollection.Find(s =>
                s.HasPainting ||
                s.PaintStartConfirmedAt != null ||
                s.PaintTimeHours > 0 ||
                !string.IsNullOrWhiteSpace(s.PaintResponsible)).ToList());

        public Task<List<Sale>> GetServiceScheduleAsync() =>
            Task.FromResult(_salesCollection.Find(s =>
                s.HasPainting ||
                s.PaintStartConfirmedAt != null ||
                s.PaintTimeHours > 0 ||
                !string.IsNullOrWhiteSpace(s.PaintResponsible) ||
                s.HasCustomArt ||
                s.DesignStartConfirmedAt != null ||
                s.DesignTimeHours > 0 ||
                !string.IsNullOrWhiteSpace(s.DesignResponsible) ||
                s.DesignValue.HasValue).ToList());

        private static string NormalizeServiceStatus(string? value)
        {
            return string.Equals(value, "Concluded", StringComparison.OrdinalIgnoreCase)
                ? "Concluded"
                : "Active";
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
            // Calculate Priority based on DeliveryDate
            if (newSale.DeliveryDate.HasValue)
            {
                var daysUntilDelivery = (newSale.DeliveryDate.Value.Date - DateTime.Now.Date).TotalDays;
                if (daysUntilDelivery <= 0) newSale.Priority = 0; // Overdue or Today
                else if (daysUntilDelivery <= 1) newSale.Priority = 1; // Tomorrow
                else if (daysUntilDelivery <= 3) newSale.Priority = 2; // Within 3 days
                else if (daysUntilDelivery <= 7) newSale.Priority = 3; // Within a week
                else newSale.Priority = 5; // More than a week
            }
            else
            {
                newSale.Priority = 10; // No date, lowest priority
            }

            if (newSale.PrintStatus == "InProgress" || newSale.PrintStatus == "Staged")
            {
                var currentPrint = _salesCollection.FindOne(s => s.PrintStatus == "InProgress" || s.PrintStatus == "Staged");
                if (currentPrint != null)
                {
                    throw new InvalidOperationException("Only one sale can be InProgress or Staged");
                }
                if (newSale.PrintStatus == "InProgress")
                {
                    newSale.PrintStartedAt = DateTime.Now;
                }
            }

            if (newSale.FilamentId != null)
            {
                var filament = await _filamentService.GetAsync(newSale.FilamentId.ToString());
                if (filament != null)
                {
                    filament.RemainingMassGrams -= newSale.MassGrams;
                    await _filamentService.UpdateAsync(filament.Id!.ToString(), filament);
                }
            }

            NormalizePaintingFields(newSale);
            NormalizeDesignFields(newSale);
            newSale.DesignStatus = NormalizeServiceStatus(newSale.DesignStatus);
            newSale.PaintStatus = NormalizeServiceStatus(newSale.PaintStatus);
            ValidateDesignSchedule(newSale);
            ValidatePaintSchedule(newSale);
            _salesCollection.Insert(newSale);
        }

        public async Task UpdateAsync(string id, Sale updatedSale)
        {
            var existingSale = _salesCollection.FindById(new ObjectId(id));
            if (existingSale is null)
            {
                throw new InvalidOperationException("Sale not found");
            }

            // Recalculate Priority on Update
            if (updatedSale.DeliveryDate.HasValue)
            {
                var daysUntilDelivery = (updatedSale.DeliveryDate.Value.Date - DateTime.Now.Date).TotalDays;
                if (daysUntilDelivery <= 0) updatedSale.Priority = 0;
                else if (daysUntilDelivery <= 1) updatedSale.Priority = 1;
                else if (daysUntilDelivery <= 3) updatedSale.Priority = 2;
                else if (daysUntilDelivery <= 7) updatedSale.Priority = 3;
                else updatedSale.Priority = 5;
            }
            else
            {
                updatedSale.Priority = 10;
            }

            if (updatedSale.PrintStatus == "InProgress" || updatedSale.PrintStatus == "Staged")
            {
                var currentPrint = _salesCollection.FindOne(s => s.PrintStatus == "InProgress" || s.PrintStatus == "Staged");
                if (currentPrint != null && currentPrint.Id!.ToString() != id)
                {
                    throw new InvalidOperationException("Only one sale can be InProgress or Staged");
                }
                
                // If transitioning to InProgress, set start time if not set
                if (existingSale != null && updatedSale.PrintStatus == "InProgress" && existingSale.PrintStatus != "InProgress")
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
            ValidateDesignSchedule(updatedSale);
            ValidatePaintSchedule(updatedSale);
            await AdjustFilamentStockAsync(existingSale, updatedSale);
            _salesCollection.Update(updatedSale);
        }

        private async Task AdjustFilamentStockAsync(Sale existingSale, Sale updatedSale)
        {
            var oldFilamentId = existingSale.FilamentId?.ToString();
            var newFilamentId = updatedSale.FilamentId?.ToString();
            var oldMass = existingSale.MassGrams;
            var newMass = updatedSale.MassGrams;

            if (string.IsNullOrWhiteSpace(oldFilamentId) && string.IsNullOrWhiteSpace(newFilamentId))
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(oldFilamentId) && string.IsNullOrWhiteSpace(newFilamentId))
            {
                var oldFilament = await _filamentService.GetAsync(oldFilamentId);
                if (oldFilament != null)
                {
                    oldFilament.RemainingMassGrams += oldMass;
                    await _filamentService.UpdateAsync(oldFilament.Id!.ToString(), oldFilament);
                }
                return;
            }

            if (string.IsNullOrWhiteSpace(oldFilamentId) && !string.IsNullOrWhiteSpace(newFilamentId))
            {
                var newFilament = await _filamentService.GetAsync(newFilamentId);
                if (newFilament != null)
                {
                    newFilament.RemainingMassGrams -= newMass;
                    await _filamentService.UpdateAsync(newFilament.Id!.ToString(), newFilament);
                }
                return;
            }

            if (oldFilamentId == newFilamentId)
            {
                var delta = newMass - oldMass;
                if (Math.Abs(delta) < 0.0001d)
                {
                    return;
                }
                var filament = await _filamentService.GetAsync(oldFilamentId!);
                if (filament != null)
                {
                    filament.RemainingMassGrams -= delta;
                    await _filamentService.UpdateAsync(filament.Id!.ToString(), filament);
                }
                return;
            }

            var oldFilamentSwap = await _filamentService.GetAsync(oldFilamentId!);
            if (oldFilamentSwap != null)
            {
                oldFilamentSwap.RemainingMassGrams += oldMass;
                await _filamentService.UpdateAsync(oldFilamentSwap.Id!.ToString(), oldFilamentSwap);
            }

            var newFilamentSwap = await _filamentService.GetAsync(newFilamentId!);
            if (newFilamentSwap != null)
            {
                newFilamentSwap.RemainingMassGrams -= newMass;
                await _filamentService.UpdateAsync(newFilamentSwap.Id!.ToString(), newFilamentSwap);
            }
        }

        public Task UpdateScheduleAsync(string id, DateTime? printStartConfirmedAt)
        {
            var sale = _salesCollection.FindById(new ObjectId(id));
            if (sale is null)
            {
                throw new InvalidOperationException("Sale not found");
            }

            sale.PrintStartConfirmedAt = printStartConfirmedAt;
            ValidateDesignSchedule(sale);
            AdjustPaintScheduleAfterPrintChange(sale);
            _salesCollection.Update(sale);
            return Task.CompletedTask;
        }

        public Task UpdatePaintScheduleAsync(string id, DateTime? paintStartConfirmedAt, double? paintTimeHours, string? paintResponsible)
        {
            var sale = _salesCollection.FindById(new ObjectId(id));
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
            _salesCollection.Update(sale);
            return Task.CompletedTask;
        }

        public Task UpdateDesignScheduleAsync(string id, DateTime? designStartConfirmedAt, double? designTimeHours, string? designResponsible, decimal? designValue)
        {
            var sale = _salesCollection.FindById(new ObjectId(id));
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
            _salesCollection.Update(sale);
            return Task.CompletedTask;
        }

        public Task UpdateDesignStatusAsync(string id, string? designStatus)
        {
            var sale = _salesCollection.FindById(new ObjectId(id));
            if (sale is null)
            {
                throw new InvalidOperationException("Sale not found");
            }

            sale.DesignStatus = NormalizeServiceStatus(designStatus);
            _salesCollection.Update(sale);
            return Task.CompletedTask;
        }

        public Task UpdatePaintStatusAsync(string id, string? paintStatus)
        {
            var sale = _salesCollection.FindById(new ObjectId(id));
            if (sale is null)
            {
                throw new InvalidOperationException("Sale not found");
            }

            sale.PaintStatus = NormalizeServiceStatus(paintStatus);
            _salesCollection.Update(sale);
            return Task.CompletedTask;
        }

        public async Task RemoveAsync(string id)
        {
            var sale = await GetByIdAsync(id);
            if (sale != null && sale.FilamentId != null)
            {
                var filament = await _filamentService.GetAsync(sale.FilamentId.ToString());
                if (filament != null)
                {
                    filament.RemainingMassGrams += sale.MassGrams;
                    await _filamentService.UpdateAsync(filament.Id!.ToString(), filament);
                }
            }
            _salesCollection.Delete(new ObjectId(id));
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
            var currentPrint = _salesCollection.FindOne(s => s.PrintStatus == "InProgress" || s.PrintStatus == "Staged");
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

            foreach (var sale in queue.Where(sale => sale.PrintStartConfirmedAt.HasValue))
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

            foreach (var sale in queue.Where(sale => !sale.PrintStartConfirmedAt.HasValue))
            {
                var duration = Math.Max(GetSaleDurationHours(sale), 0);
                if (duration <= 0)
                {
                    if (sale.PrintStartScheduledAt != null)
                    {
                        sale.PrintStartScheduledAt = null;
                        _salesCollection.Update(sale);
                    }
                    continue;
                }

                var start = FindNextAvailableStart(baseTime, occupied, duration);
                if (sale.PrintStartScheduledAt != start)
                {
                    sale.PrintStartScheduledAt = start;
                    _salesCollection.Update(sale);
                }
                var end = start.AddHours(duration);
                occupied.Add((start, end));
            }

            return queue;
        }
    }
}
