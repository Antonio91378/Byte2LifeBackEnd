using Byte2Life.API.Models;
using LiteDB;

namespace Byte2Life.API.Services
{
    public class ClientService : IClientService
    {
        private readonly LiteDatabase _database;
        private readonly ILiteCollection<Client> _clientsCollection;

        public ClientService(LiteDatabase database)
        {
            _database = database;
            _clientsCollection = _database.GetCollection<Client>("Clients");
        }

        public Task<List<Client>> GetAsync(string? name = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Task.FromResult(_clientsCollection.FindAll().ToList());
            }
            
            // Case insensitive search
            return Task.FromResult(_clientsCollection.Find(c => c.Name.ToLower().Contains(name.ToLower())).ToList());
        }

        public Task<Client?> GetByIdAsync(string id) =>
            Task.FromResult<Client?>(_clientsCollection.FindById(new ObjectId(id)));

        public Task CreateAsync(Client newClient)
        {
            _clientsCollection.Insert(newClient);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(string id, Client updatedClient)
        {
            _clientsCollection.Update(updatedClient);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string id)
        {
            var salesCollection = _database.GetCollection<Sale>("Sales");
            var hasSales = salesCollection.Exists(s => s.ClientId == new ObjectId(id));

            if (hasSales)
            {
                throw new InvalidOperationException("Cannot delete client with associated sales.");
            }

            _clientsCollection.Delete(new ObjectId(id));
            return Task.CompletedTask;
        }
    }
}
