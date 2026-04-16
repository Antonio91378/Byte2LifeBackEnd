using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Byte2Life.API.Converters;
using Byte2Life.API.Models;
using Byte2Life.API.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Xunit;

namespace Byte2Life.API.Tests.IntegrationTests
{
    public class SaleIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>, IDisposable
    {
        private readonly CustomWebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly IMongoDatabase _db;
        private readonly JsonSerializerOptions _jsonOptions;

        public SaleIntegrationTests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
            
            var scope = factory.Services.CreateScope();
            _db = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            _jsonOptions.Converters.Add(new ObjectIdConverter());
        }

        public void Dispose()
        {
            _db.GetCollection<Sale>(MongoCollectionNames.Sales).DeleteMany(Builders<Sale>.Filter.Empty);
            _db.GetCollection<Filament>(MongoCollectionNames.Filaments).DeleteMany(Builders<Filament>.Filter.Empty);
            _db.GetCollection<Client>(MongoCollectionNames.Clients).DeleteMany(Builders<Client>.Filter.Empty);
        }

        [Fact]
        public async Task CreateSale_ReturnsCreatedSale_AndUpdatesFilamentMass()
        {
            // Arrange - Create Filament first
            var newFilament = new Filament 
            { 
                Description = "PLA Blue", 
                Color = "Blue", 
                Price = 200.00m, 
                InitialMassGrams = 1000, 
                RemainingMassGrams = 1000
            };
            var filamentResponse = await _client.PostAsJsonAsync("/api/filaments", newFilament, _jsonOptions);
            filamentResponse.EnsureSuccessStatusCode();
            var createdFilament = await filamentResponse.Content.ReadFromJsonAsync<Filament>(_jsonOptions);
            
            // Arrange - Create Sale
            var newSale = new Sale 
            { 
                Description = "3D Print Job", 
                MassGrams = 100, 
                Cost = 20.00m, 
                SaleValue = 50.00m,
                FilamentId = createdFilament!.Id
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/sales", newSale, _jsonOptions);

            // Assert - Sale Created
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var createdSale = await response.Content.ReadFromJsonAsync<Sale>(_jsonOptions);
            createdSale.Should().NotBeNull();
            createdSale!.Description.Should().Be(newSale.Description);

            // Assert - Filament Updated
            var getFilamentResponse = await _client.GetAsync($"/api/filaments/{createdFilament.Id}");
            var updatedFilament = await getFilamentResponse.Content.ReadFromJsonAsync<Filament>(_jsonOptions);
            updatedFilament!.RemainingMassGrams.Should().Be(900); // 1000 - 100
        }

        [Fact]
        public async Task GetSales_WithMonthFilter_ReturnsOnlySalesInMonth()
        {
            // Arrange
            var sale1 = new Sale { Description = "Sale Jan", SaleDate = new DateTime(2023, 01, 15), SaleValue = 10 };
            var sale2 = new Sale { Description = "Sale Feb", SaleDate = new DateTime(2023, 02, 15), SaleValue = 20 };
            var sale3 = new Sale { Description = "Sale Jan 2", SaleDate = new DateTime(2023, 01, 20), SaleValue = 30 };

            await _client.PostAsJsonAsync("/api/sales", sale1, _jsonOptions);
            await _client.PostAsJsonAsync("/api/sales", sale2, _jsonOptions);
            await _client.PostAsJsonAsync("/api/sales", sale3, _jsonOptions);

            // Act
            var response = await _client.GetAsync("/api/sales?date=2023-01");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var sales = await response.Content.ReadFromJsonAsync<List<Sale>>(_jsonOptions);
            
            sales.Should().HaveCount(2);
            sales.Should().Contain(s => s.Description == "Sale Jan");
            sales.Should().Contain(s => s.Description == "Sale Jan 2");
            sales.Should().NotContain(s => s.Description == "Sale Feb");
        }

        [Fact]
        public async Task UpdateSale_UpdatesStatus_ShouldSucceed()
        {
            // Arrange
            var newSale = new Sale 
            { 
                Description = "Status Update Test", 
                PrintStatus = "Pending"
            };
            var createResponse = await _client.PostAsJsonAsync("/api/sales", newSale, _jsonOptions);
            var createdSale = await createResponse.Content.ReadFromJsonAsync<Sale>(_jsonOptions);

            createdSale!.PrintStatus = "InQueue";

            // Act
            var updateResponse = await _client.PutAsJsonAsync($"/api/sales/{createdSale.Id}", createdSale, _jsonOptions);

            // Assert
            updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var getResponse = await _client.GetAsync($"/api/sales/{createdSale.Id}");
            var updatedSale = await getResponse.Content.ReadFromJsonAsync<Sale>(_jsonOptions);
            updatedSale!.PrintStatus.Should().Be("InQueue");
        }
    }
}
