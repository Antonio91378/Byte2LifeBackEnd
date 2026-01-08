using Byte2Life.API.Models;
using LiteDB;

namespace Byte2Life.API.Services
{
    public class PaintingTaskService : IPaintingTaskService
    {
        private readonly ILiteCollection<PaintingTask> _collection;

        public PaintingTaskService(LiteDatabase database)
        {
            _collection = database.GetCollection<PaintingTask>("PaintingTasks");
        }

        public Task<List<PaintingTask>> GetAllAsync()
        {
            var items = _collection.FindAll()
                .OrderBy(t => t.StartAt ?? DateTime.MaxValue)
                .ToList();
            return Task.FromResult(items);
        }

        public Task<PaintingTask?> GetByIdAsync(string id) =>
            Task.FromResult<PaintingTask?>(_collection.FindById(new ObjectId(id)));

        public Task CreateAsync(PaintingTask task)
        {
            task.CreatedAt = DateTime.UtcNow;
            task.UpdatedAt = null;
            _collection.Insert(task);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(string id, PaintingTask task)
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
