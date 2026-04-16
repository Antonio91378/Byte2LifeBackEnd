using ServiceProviderModel = Byte2Life.API.Models.ServiceProvider;
using Byte2Life.API.Persistence;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Byte2Life.API.Services
{
    public class ServiceProviderService : IServiceProviderService
    {
        private readonly IMongoCollection<ServiceProviderModel> _collection;

        public ServiceProviderService(IMongoDatabase database)
        {
            _collection = database.GetCollection<ServiceProviderModel>(MongoCollectionNames.ServiceProviders);
        }

        public Task<List<ServiceProviderModel>> GetAsync()
        {
            var items = _collection.Find(FilterDefinition<ServiceProviderModel>.Empty).ToList();
            foreach (var provider in items)
            {
                NormalizeCategories(provider);
            }
            return Task.FromResult(items);
        }

        public Task<ServiceProviderModel?> GetAsync(string id)
        {
            var item = _collection.Find(MongoId.FilterById<ServiceProviderModel>(id)).FirstOrDefault();
            if (item != null)
            {
                NormalizeCategories(item);
            }
            return Task.FromResult<ServiceProviderModel?>(item);
        }

        public Task CreateAsync(ServiceProviderModel provider)
        {
            NormalizeCategories(provider);
            if (!provider.Id.HasValue || provider.Id.Value == ObjectId.Empty)
            {
                provider.Id = MongoId.New();
            }

            _collection.InsertOne(provider);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(string id, ServiceProviderModel provider)
        {
            NormalizeCategories(provider);
            provider.Id = MongoId.Parse(id);
            _collection.ReplaceOne(MongoId.FilterById<ServiceProviderModel>(id), provider);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string id)
        {
            _collection.DeleteOne(MongoId.FilterById<ServiceProviderModel>(id));
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
