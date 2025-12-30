using Byte2Life.API.Models;
using LiteDB;

namespace Byte2Life.API.Services
{
    public class InvestmentService : IInvestmentService
    {
        private readonly LiteDatabase _database;
        private readonly ILiteCollection<Investment> _collection;

        public InvestmentService(LiteDatabase database)
        {
            _database = database;
            _collection = _database.GetCollection<Investment>("Investments");
        }

        public Task<List<Investment>> GetAllAsync()
        {
            return Task.FromResult(_collection.FindAll().OrderByDescending(x => x.Date).ToList());
        }

        public Task<Investment?> GetByIdAsync(string id)
        {
            return Task.FromResult<Investment?>(_collection.FindById(new ObjectId(id)));
        }

        public Task CreateAsync(Investment investment)
        {
            _collection.Insert(investment);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(string id, Investment investment)
        {
            var existing = _collection.FindById(new ObjectId(id));
            if (existing != null)
            {
                investment.Id = new ObjectId(id);
                _collection.Update(investment);
            }
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string id)
        {
            _collection.Delete(new ObjectId(id));
            return Task.CompletedTask;
        }

        public Task<decimal> GetTotalInvestmentAsync()
        {
            var all = _collection.FindAll();
            return Task.FromResult(all.Sum(x => x.Amount));
        }
    }
}
