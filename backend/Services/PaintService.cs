using Byte2Life.API.Models;
using Byte2Life.API.Persistence;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Byte2Life.API.Services
{
    public class PaintService : IPaintService
    {
        private readonly IMongoCollection<Paint> _paintsCollection;

        public PaintService(IMongoDatabase database)
        {
            _paintsCollection = database.GetCollection<Paint>(MongoCollectionNames.Paints);
        }

        public Task<List<Paint>> GetAsync() =>
            Task.FromResult(_paintsCollection.Find(FilterDefinition<Paint>.Empty).ToList());

        public Task<Paint?> GetAsync(string id) =>
            Task.FromResult(FindPaintById(id));

        private Paint? FindPaintById(string id)
        {
            return _paintsCollection.Find(MongoId.FilterById<Paint>(id)).FirstOrDefault();
        }

        public Task CreateAsync(Paint newPaint)
        {
            if (!newPaint.Id.HasValue || newPaint.Id.Value == ObjectId.Empty)
            {
                newPaint.Id = MongoId.New();
            }

            _paintsCollection.InsertOne(newPaint);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(string id, Paint updatedPaint)
        {
            updatedPaint.Id = MongoId.Parse(id);
            _paintsCollection.ReplaceOne(MongoId.FilterById<Paint>(id), updatedPaint);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string id)
        {
            _paintsCollection.DeleteOne(MongoId.FilterById<Paint>(id));
            return Task.CompletedTask;
        }
    }
}
