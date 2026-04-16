using Byte2Life.API.Models;
using Byte2Life.API.Persistence;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Byte2Life.API.Services
{
    public class PaintingTaskService : IPaintingTaskService
    {
        private readonly IMongoCollection<PaintingTask> _collection;

        public PaintingTaskService(IMongoDatabase database)
        {
            _collection = database.GetCollection<PaintingTask>(MongoCollectionNames.PaintingTasks);
        }

        public Task<List<PaintingTask>> GetAllAsync()
        {
            var items = _collection.Find(FilterDefinition<PaintingTask>.Empty).ToList()
                .OrderBy(t => t.StartAt ?? DateTime.MaxValue)
                .ToList();
            return Task.FromResult(items);
        }

        public Task<PaintingTask?> GetByIdAsync(string id) =>
            Task.FromResult(FindPaintingTaskById(id));

        private PaintingTask? FindPaintingTaskById(string id)
        {
            return _collection.Find(MongoId.FilterById<PaintingTask>(id)).FirstOrDefault();
        }

        public Task CreateAsync(PaintingTask task)
        {
            if (string.IsNullOrWhiteSpace(task.Status))
            {
                task.Status = "Active";
            }
            if (!task.Id.HasValue || task.Id.Value == ObjectId.Empty)
            {
                task.Id = MongoId.New();
            }
            task.CreatedAt = DateTime.UtcNow;
            task.UpdatedAt = null;
            _collection.InsertOne(task);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(string id, PaintingTask task)
        {
            var existing = _collection.Find(MongoId.FilterById<PaintingTask>(id)).FirstOrDefault();
            if (existing != null)
            {
                task.Id = existing.Id;
                task.CreatedAt = existing.CreatedAt;
                if (string.IsNullOrWhiteSpace(task.Status))
                {
                    task.Status = string.IsNullOrWhiteSpace(existing.Status) ? "Active" : existing.Status;
                }
                task.UpdatedAt = DateTime.UtcNow;
                _collection.ReplaceOne(MongoId.FilterById<PaintingTask>(id), task);
            }
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string id)
        {
            _collection.DeleteOne(MongoId.FilterById<PaintingTask>(id));
            return Task.CompletedTask;
        }
    }
}
