using ServiceProviderModel = Byte2Life.API.Models.ServiceProvider;
using LiteDB;

namespace Byte2Life.API.Services
{
    public class ServiceProviderService : IServiceProviderService
    {
        private readonly ILiteCollection<ServiceProviderModel> _collection;

        public ServiceProviderService(LiteDatabase database)
        {
            _collection = database.GetCollection<ServiceProviderModel>("ServiceProviders");
        }

        public Task<List<ServiceProviderModel>> GetAsync()
        {
            var items = _collection.FindAll().ToList();
            foreach (var provider in items)
            {
                NormalizeCategories(provider);
            }
            return Task.FromResult(items);
        }

        public Task<ServiceProviderModel?> GetAsync(string id)
        {
            var item = _collection.FindById(new ObjectId(id));
            if (item != null)
            {
                NormalizeCategories(item);
            }
            return Task.FromResult<ServiceProviderModel?>(item);
        }

        public Task CreateAsync(ServiceProviderModel provider)
        {
            NormalizeCategories(provider);
            _collection.Insert(provider);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(string id, ServiceProviderModel provider)
        {
            NormalizeCategories(provider);
            _collection.Update(provider);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string id)
        {
            _collection.Delete(new ObjectId(id));
            return Task.CompletedTask;
        }

        private static void NormalizeCategories(ServiceProviderModel provider)
        {
            var categories = provider.Categories ?? new List<string>();
            if (categories.Count == 0 && !string.IsNullOrWhiteSpace(provider.Category))
            {
                categories = new List<string> { provider.Category };
            }

            provider.Categories = categories
                .Select(category => category?.Trim() ?? string.Empty)
                .Where(category => category != string.Empty)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            provider.Category = null;
        }
    }
}
