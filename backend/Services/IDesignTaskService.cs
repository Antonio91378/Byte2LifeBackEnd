using Byte2Life.API.Models;

namespace Byte2Life.API.Services
{
    public interface IDesignTaskService
    {
        Task<List<DesignTask>> GetAllAsync();
        Task<DesignTask?> GetByIdAsync(string id);
        Task CreateAsync(DesignTask task);
        Task UpdateAsync(string id, DesignTask task);
        Task RemoveAsync(string id);
    }
}
