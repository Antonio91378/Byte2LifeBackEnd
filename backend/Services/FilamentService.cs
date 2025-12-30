using Byte2Life.API.Models;
using LiteDB;

namespace Byte2Life.API.Services
{
    public class FilamentService : IFilamentService
    {
        private readonly LiteDatabase _database;
        private readonly ILiteCollection<Filament> _filamentsCollection;

        public FilamentService(LiteDatabase database)
        {
            _database = database;
            _filamentsCollection = _database.GetCollection<Filament>("Filaments");
        }

        public Task<List<Filament>> GetAsync() =>
            Task.FromResult(_filamentsCollection.FindAll().ToList());

        public Task<Filament?> GetAsync(string id) =>
            Task.FromResult<Filament?>(_filamentsCollection.FindById(new ObjectId(id)));

        public Task CreateAsync(Filament newFilament)
        {
            _filamentsCollection.Insert(newFilament);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(string id, Filament updatedFilament)
        {
            _filamentsCollection.Update(updatedFilament);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string id)
        {
            var salesCollection = _database.GetCollection<Sale>("Sales");
            var hasSales = salesCollection.Exists(s => s.FilamentId == new ObjectId(id));

            if (hasSales)
            {
                throw new InvalidOperationException("Cannot delete filament with associated sales.");
            }

            _filamentsCollection.Delete(new ObjectId(id));
            return Task.CompletedTask;
        }
    }
}
