using Byte2Life.API.Models;

namespace Byte2Life.API.Services
{
    public interface IPaintService
    {
        Task<List<Paint>> GetAsync();
        Task<Paint?> GetAsync(string id);
        Task CreateAsync(Paint newPaint);
        Task UpdateAsync(string id, Paint updatedPaint);
        Task RemoveAsync(string id);
    }
}
