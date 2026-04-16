using Byte2Life.API.Models;
using Byte2Life.API.Persistence;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Byte2Life.API.Services
{
    public class InvestmentService : IInvestmentService
    {
        private readonly IMongoCollection<Investment> _collection;

        public InvestmentService(IMongoDatabase database)
        {
            _collection = database.GetCollection<Investment>(MongoCollectionNames.Investments);
        }

        public Task<List<Investment>> GetAllAsync()
        {
            return Task.FromResult(_collection.Find(FilterDefinition<Investment>.Empty).ToList().OrderByDescending(x => x.Date).ToList());
        }

        public Task<Investment?> GetByIdAsync(string id)
        {
            return Task.FromResult(FindInvestmentById(id));
        }

        private Investment? FindInvestmentById(string id)
        {
            return _collection.Find(MongoId.FilterById<Investment>(id)).FirstOrDefault();
        }

        public Task CreateAsync(Investment investment)
        {
            if (!investment.Id.HasValue || investment.Id.Value == ObjectId.Empty)
            {
                investment.Id = MongoId.New();
            }

            _collection.InsertOne(investment);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(string id, Investment investment)
        {
            var existing = _collection.Find(MongoId.FilterById<Investment>(id)).FirstOrDefault();
            if (existing != null)
            {
                investment.Id = existing.Id;
                _collection.ReplaceOne(MongoId.FilterById<Investment>(id), investment);
            }
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string id)
        {
            _collection.DeleteOne(MongoId.FilterById<Investment>(id));
            return Task.CompletedTask;
        }

        public Task<decimal> GetTotalInvestmentAsync()
        {
            var all = _collection.Find(FilterDefinition<Investment>.Empty).ToList();
            return Task.FromResult(all.Sum(x => x.Amount));
        }
    }
}
