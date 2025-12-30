using Byte2Life.API.Models;

namespace Byte2Life.API.Services
{
    public interface IInvestmentService
    {
        Task<List<Investment>> GetAllAsync();
        Task<Investment?> GetByIdAsync(string id);
        Task CreateAsync(Investment investment);
        Task UpdateAsync(string id, Investment investment);
        Task DeleteAsync(string id);
        Task<decimal> GetTotalInvestmentAsync();
    }
}
