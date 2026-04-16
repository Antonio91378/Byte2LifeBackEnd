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
    public class FilamentIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>, IDisposable
    {
        private readonly CustomWebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly IMongoDatabase _db;
        private readonly JsonSerializerOptions _jsonOptions;

        public FilamentIntegrationTests(CustomWebApplicationFactory<Program> factory)
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
            _db.GetCollection<Filament>(MongoCollectionNames.Filaments).DeleteMany(Builders<Filament>.Filter.Empty);
        }

        [Fact]
        public async Task GetFilaments_ReturnsEmptyList_WhenNoFilamentsExist()
        {
            // Act
            var response = await _client.GetAsync("/api/filaments");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var filaments = await response.Content.ReadFromJsonAsync<List<Filament>>(_jsonOptions);
            filaments.Should().BeEmpty();
        }

        [Fact]
        public async Task CreateFilament_ReturnsCreatedFilament()
        {
            // Arrange
            var newFilament = new Filament 
            { 
                Description = "PLA Red", 
                Color = "Red", 
                Price = 150.00m, 
                InitialMassGrams = 1000, 
                RemainingMassGrams = 1000,
                Link = "http://example.com/filament"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/filaments", newFilament, _jsonOptions);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var createdFilament = await response.Content.ReadFromJsonAsync<Filament>(_jsonOptions);
            createdFilament.Should().NotBeNull();
            createdFilament!.Description.Should().Be(newFilament.Description);
            createdFilament.Id.Should().NotBeNull();
        }
    }
}
