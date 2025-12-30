using Byte2Life.API.Models;
using LiteDB;

namespace Byte2Life.API.Services
{
    public class SaleService : ISaleService
    {
        private readonly LiteDatabase _database;
        private readonly ILiteCollection<Sale> _salesCollection;
        private readonly IFilamentService _filamentService;

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
            Task.FromResult(_salesCollection.Find(s => s.PrintStatus == "InQueue")
                .OrderBy(s => s.Priority)
                .ToList());

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

            _salesCollection.Insert(newSale);
        }

        public Task UpdateAsync(string id, Sale updatedSale)
        {
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
                var existingSale = _salesCollection.FindById(new ObjectId(id));
                if (existingSale != null && updatedSale.PrintStatus == "InProgress" && existingSale.PrintStatus != "InProgress")
                {
                    updatedSale.PrintStartedAt = DateTime.Now;
                }
            }

            _salesCollection.Update(updatedSale);
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
    }
}
