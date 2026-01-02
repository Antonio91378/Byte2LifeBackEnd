using Byte2Life.API.Models;
using LiteDB;

namespace Byte2Life.API.Services
{
    public class ReminderService : IReminderService
    {
        private readonly ILiteCollection<Reminder> _collection;

        public ReminderService(LiteDatabase database)
        {
            _collection = database.GetCollection<Reminder>("Reminders");
        }

        public Task<List<Reminder>> GetAllAsync()
        {
            var items = _collection.FindAll()
                .OrderBy(r => r.IsDone)
                .ThenByDescending(r => r.CreatedAt)
                .ToList();
            return Task.FromResult(items);
        }

        public Task<Reminder?> GetByIdAsync(string id)
        {
            return Task.FromResult<Reminder?>(_collection.FindById(new ObjectId(id)));
        }

        public Task CreateAsync(Reminder reminder)
        {
            reminder.CreatedAt = DateTime.UtcNow;
            reminder.UpdatedAt = null;
            _collection.Insert(reminder);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(string id, Reminder reminder)
        {
            var existing = _collection.FindById(new ObjectId(id));
            if (existing != null)
            {
                reminder.Id = existing.Id;
                reminder.CreatedAt = existing.CreatedAt;
                reminder.UpdatedAt = DateTime.UtcNow;
                _collection.Update(reminder);
            }
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string id)
        {
            _collection.Delete(new ObjectId(id));
            return Task.CompletedTask;
        }
    }
}
