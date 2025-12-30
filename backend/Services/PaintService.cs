using Byte2Life.API.Models;
using LiteDB;

namespace Byte2Life.API.Services
{
    public class PaintService : IPaintService
    {
        private readonly LiteDatabase _database;
        private readonly ILiteCollection<Paint> _paintsCollection;

        public PaintService(LiteDatabase database)
        {
            _database = database;
            _paintsCollection = _database.GetCollection<Paint>("Paints");
        }

        public Task<List<Paint>> GetAsync() =>
            Task.FromResult(_paintsCollection.FindAll().ToList());

        public Task<Paint?> GetAsync(string id) =>
            Task.FromResult<Paint?>(_paintsCollection.FindById(new ObjectId(id)));

        public Task CreateAsync(Paint newPaint)
        {
            _paintsCollection.Insert(newPaint);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(string id, Paint updatedPaint)
        {
            _paintsCollection.Update(updatedPaint);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string id)
        {
            _paintsCollection.Delete(new ObjectId(id));
            return Task.CompletedTask;
        }
    }
}
