using Byte2Life.API.Models;
using LiteDB;

namespace Byte2Life.API.Services
{
    public class DesignTaskService : IDesignTaskService
    {
        private readonly ILiteCollection<DesignTask> _collection;

        public DesignTaskService(LiteDatabase database)
        {
            _collection = database.GetCollection<DesignTask>("DesignTasks");
        }

        public Task<List<DesignTask>> GetAllAsync()
        {
            var items = _collection.FindAll()
                .OrderBy(t => t.StartAt ?? DateTime.MaxValue)
                .ToList();
            return Task.FromResult(items);
        }

        public Task<DesignTask?> GetByIdAsync(string id) =>
            Task.FromResult<DesignTask?>(_collection.FindById(new ObjectId(id)));

        public Task CreateAsync(DesignTask task)
        {
            task.CreatedAt = DateTime.UtcNow;
            task.UpdatedAt = null;
            _collection.Insert(task);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(string id, DesignTask task)
        {
            var existing = _collection.FindById(new ObjectId(id));
            if (existing != null)
            {
                task.Id = existing.Id;
                task.CreatedAt = existing.CreatedAt;
                task.UpdatedAt = DateTime.UtcNow;
                _collection.Update(task);
            }
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string id)
        {
            _collection.Delete(new ObjectId(id));
            return Task.CompletedTask;
        }
    }
}
