using Byte2Life.API.Models;

namespace Byte2Life.API.Services
{
    public interface IReminderService
    {
        Task<List<Reminder>> GetAllAsync();
        Task<Reminder?> GetByIdAsync(string id);
        Task CreateAsync(Reminder reminder);
        Task UpdateAsync(string id, Reminder reminder);
        Task DeleteAsync(string id);
    }
}
