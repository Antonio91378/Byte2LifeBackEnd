using Byte2Life.API.Models;
using Byte2Life.API.Persistence;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Byte2Life.API.Services
{
    public class ReminderService : IReminderService
    {
        private readonly IMongoCollection<Reminder> _collection;

        public ReminderService(IMongoDatabase database)
        {
            _collection = database.GetCollection<Reminder>(MongoCollectionNames.Reminders);
        }

        public Task<List<Reminder>> GetAllAsync()
        {
            var items = _collection.Find(FilterDefinition<Reminder>.Empty).ToList()
                .OrderBy(r => r.IsDone)
                .ThenByDescending(r => r.CreatedAt)
                .ToList();
            return Task.FromResult(items);
        }

        public Task<Reminder?> GetByIdAsync(string id)
        {
            return Task.FromResult(FindReminderById(id));
        }

        private Reminder? FindReminderById(string id)
        {
            return _collection.Find(MongoId.FilterById<Reminder>(id)).FirstOrDefault();
        }

        public Task CreateAsync(Reminder reminder)
        {
            if (!reminder.Id.HasValue || reminder.Id.Value == ObjectId.Empty)
            {
                reminder.Id = MongoId.New();
            }
            reminder.CreatedAt = DateTime.UtcNow;
            reminder.UpdatedAt = null;
            _collection.InsertOne(reminder);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(string id, Reminder reminder)
        {
            var existing = _collection.Find(MongoId.FilterById<Reminder>(id)).FirstOrDefault();
            if (existing != null)
            {
                reminder.Id = existing.Id;
                reminder.CreatedAt = existing.CreatedAt;
                reminder.UpdatedAt = DateTime.UtcNow;
                _collection.ReplaceOne(MongoId.FilterById<Reminder>(id), reminder);
            }
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string id)
        {
            _collection.DeleteOne(MongoId.FilterById<Reminder>(id));
            return Task.CompletedTask;
        }
    }
}
