using Byte2Life.API.Models;

namespace Byte2Life.API.Services
{
    public interface ISaleService
    {
        Task<List<Sale>> GetAsync(string? dateFilter = null);
        Task<Sale?> GetByIdAsync(string id);
        Task<List<Sale>> GetByFilamentIdAsync(string filamentId);
        Task<Sale?> GetCurrentPrintAsync();
        Task<List<Sale>> GetQueueAsync();
        Task<List<Sale>> GetPaintingScheduleAsync();
        Task CreateAsync(Sale newSale);
        Task UpdateAsync(string id, Sale updatedSale);
        Task UpdateScheduleAsync(string id, DateTime? printStartConfirmedAt);
        Task UpdatePaintScheduleAsync(string id, DateTime? paintStartConfirmedAt, double? paintTimeHours, string? paintResponsible);
        Task RemoveAsync(string id);
    }
}
