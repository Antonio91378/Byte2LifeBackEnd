using Byte2Life.API.Models;

namespace Byte2Life.API.Services
{
    public interface IClientService
    {
        Task<List<Client>> GetAsync(string? name = null);
        Task<Client?> GetByIdAsync(string id);
        Task CreateAsync(Client newClient);
        Task UpdateAsync(string id, Client updatedClient);
        Task RemoveAsync(string id);
    }
}
