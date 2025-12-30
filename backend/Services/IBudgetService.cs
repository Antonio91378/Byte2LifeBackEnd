using Byte2Life.API.Models;

namespace Byte2Life.API.Services
{
    public interface IBudgetService
    {
        Task<BudgetResult> CalculateBudgetAsync(BudgetRequest request);
    }
}
