using Byte2Life.API.Models;
using Byte2Life.API.Persistence;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Byte2Life.API.Services
{
    public class DesignTaskService : IDesignTaskService
    {
        private readonly IMongoCollection<DesignTask> _collection;

        public DesignTaskService(IMongoDatabase database)
        {
            _collection = database.GetCollection<DesignTask>(MongoCollectionNames.DesignTasks);
        }

        public Task<List<DesignTask>> GetAllAsync()
        {
            var items = _collection.Find(FilterDefinition<DesignTask>.Empty).ToList()
                .OrderBy(t => t.StartAt ?? DateTime.MaxValue)
                .ToList();
            return Task.FromResult(items);
        }

        public Task<DesignTask?> GetByIdAsync(string id) =>
            Task.FromResult(FindDesignTaskById(id));

        private DesignTask? FindDesignTaskById(string id)
        {
            return _collection.Find(MongoId.FilterById<DesignTask>(id)).FirstOrDefault();
        }

        public Task CreateAsync(DesignTask task)
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

        public Task UpdateAsync(string id, DesignTask task)
        {
            var existing = _collection.Find(MongoId.FilterById<DesignTask>(id)).FirstOrDefault();
            if (existing != null)
            {
                task.Id = existing.Id;
                task.CreatedAt = existing.CreatedAt;
                if (string.IsNullOrWhiteSpace(task.Status))
                {
                    task.Status = string.IsNullOrWhiteSpace(existing.Status) ? "Active" : existing.Status;
                }
                task.UpdatedAt = DateTime.UtcNow;
                _collection.ReplaceOne(MongoId.FilterById<DesignTask>(id), task);
            }
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string id)
        {
            _collection.DeleteOne(MongoId.FilterById<DesignTask>(id));
            return Task.CompletedTask;
        }
    }
}
