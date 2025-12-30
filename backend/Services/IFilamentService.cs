using Byte2Life.API.Models;

namespace Byte2Life.API.Services
{
    public interface IFilamentService
    {
        Task<List<Filament>> GetAsync();
        Task<Filament?> GetAsync(string id);
        Task CreateAsync(Filament newFilament);
        Task UpdateAsync(string id, Filament updatedFilament);
        Task RemoveAsync(string id);
    }
}
