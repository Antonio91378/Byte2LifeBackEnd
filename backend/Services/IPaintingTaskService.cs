using Byte2Life.API.Models;

namespace Byte2Life.API.Services
{
    public interface IPaintingTaskService
    {
        Task<List<PaintingTask>> GetAllAsync();
        Task<PaintingTask?> GetByIdAsync(string id);
        Task CreateAsync(PaintingTask task);
        Task UpdateAsync(string id, PaintingTask task);
        Task RemoveAsync(string id);
    }
}
