using Byte2Life.API.Models;
using Byte2Life.API.Persistence;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Byte2Life.API.Services
{
    public class ClientService : IClientService
    {
        private readonly IMongoCollection<Client> _clientsCollection;
        private readonly IMongoCollection<Sale> _salesCollection;

        public ClientService(IMongoDatabase database)
        {
            _clientsCollection = database.GetCollection<Client>(MongoCollectionNames.Clients);
            _salesCollection = database.GetCollection<Sale>(MongoCollectionNames.Sales);
        }

        public Task<List<Client>> GetAsync(string? name = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Task.FromResult(_clientsCollection.Find(FilterDefinition<Client>.Empty).ToList());
            }

            var filter = Builders<Client>.Filter.Regex(client => client.Name, new BsonRegularExpression(name.Trim(), "i"));
            return Task.FromResult(_clientsCollection.Find(filter).ToList());
        }

        public Task<Client?> GetByIdAsync(string id) =>
            Task.FromResult(FindClientById(id));

        private Client? FindClientById(string id)
        {
            return _clientsCollection.Find(MongoId.FilterById<Client>(id)).FirstOrDefault();
        }

        public Task CreateAsync(Client newClient)
        {
            if (!newClient.Id.HasValue || newClient.Id.Value == ObjectId.Empty)
            {
                newClient.Id = MongoId.New();
            }

            _clientsCollection.InsertOne(newClient);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(string id, Client updatedClient)
        {
            updatedClient.Id = MongoId.Parse(id);
            _clientsCollection.ReplaceOne(MongoId.FilterById<Client>(id), updatedClient);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string id)
        {
            var objectId = MongoId.Parse(id);
            var hasSales = _salesCollection.AsQueryable().Any(sale => sale.ClientId == objectId);

            if (hasSales)
            {
                throw new InvalidOperationException("Cannot delete client with associated sales.");
            }

            _clientsCollection.DeleteOne(MongoId.FilterById<Client>(id));
            return Task.CompletedTask;
        }
    }
}
