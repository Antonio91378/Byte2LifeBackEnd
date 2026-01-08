using ServiceProviderModel = Byte2Life.API.Models.ServiceProvider;

namespace Byte2Life.API.Services
{
    public interface IServiceProviderService
    {
        Task<List<ServiceProviderModel>> GetAsync();
        Task<ServiceProviderModel?> GetAsync(string id);
        Task CreateAsync(ServiceProviderModel provider);
        Task UpdateAsync(string id, ServiceProviderModel provider);
        Task RemoveAsync(string id);
    }
}
