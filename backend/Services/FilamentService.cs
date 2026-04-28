using Byte2Life.API.Models;
using Byte2Life.API.Persistence;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Byte2Life.API.Services
{
    public class FilamentService : IFilamentService
    {
        private readonly IMongoCollection<Filament> _filamentsCollection;
        private readonly IMongoCollection<Sale> _salesCollection;

        public FilamentService(IMongoDatabase database)
        {
            _filamentsCollection = database.GetCollection<Filament>(MongoCollectionNames.Filaments);
            _salesCollection = database.GetCollection<Sale>(MongoCollectionNames.Sales);
        }

        public Task<List<Filament>> GetAsync() =>
            Task.FromResult(_filamentsCollection.Find(FilterDefinition<Filament>.Empty).ToList());

        public Task<Filament?> GetAsync(string id) =>
            Task.FromResult(FindFilamentById(id));

        private Filament? FindFilamentById(string id)
        {
            return _filamentsCollection.Find(MongoId.FilterById<Filament>(id)).FirstOrDefault();
        }

        public Task CreateAsync(Filament newFilament)
        {
            if (!newFilament.Id.HasValue || newFilament.Id.Value == ObjectId.Empty)
            {
                newFilament.Id = MongoId.New();
            }

            _filamentsCollection.InsertOne(newFilament);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(string id, Filament updatedFilament)
        {
            updatedFilament.Id = MongoId.Parse(id);
            _filamentsCollection.ReplaceOne(MongoId.FilterById<Filament>(id), updatedFilament);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string id)
        {
            var objectId = MongoId.Parse(id);
            var hasSales = _salesCollection.Find(FilterDefinition<Sale>.Empty).ToList()
                .Any(sale =>
                    sale.FilamentId == objectId ||
                    (sale.Filaments != null && sale.Filaments.Any(usage => usage.FilamentId == objectId)));

            if (hasSales)
            {
                throw new InvalidOperationException("Cannot delete filament with associated sales.");
            }

            _filamentsCollection.DeleteOne(MongoId.FilterById<Filament>(id));
            return Task.CompletedTask;
        }
    }
}
