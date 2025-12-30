using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Byte2Life.API.Converters;
using Byte2Life.API.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using LiteDB;
using Microsoft.Extensions.DependencyInjection;

namespace Byte2Life.API.Tests.IntegrationTests
{
    public class DeleteCascadeTests : IClassFixture<CustomWebApplicationFactory<Program>>, IDisposable
    {
        private readonly CustomWebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly LiteDatabase _db;
        private readonly JsonSerializerOptions _jsonOptions;

        public DeleteCascadeTests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
            
            var scope = factory.Services.CreateScope();
            _db = scope.ServiceProvider.GetRequiredService<LiteDatabase>();

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            _jsonOptions.Converters.Add(new ObjectIdConverter());
        }

        public void Dispose()
        {
            _db.GetCollection<Sale>("Sales").DeleteAll();
            _db.GetCollection<Filament>("Filaments").DeleteAll();
            _db.GetCollection<Client>("Clients").DeleteAll();
        }

        [Fact]
        public async Task DeleteFilament_WithAssociatedSale_ShouldFail()
        {
            // Arrange
            var filament = new Filament { Description = "Test Filament", Color = "Red", Price = 100, InitialMassGrams = 1000, RemainingMassGrams = 1000 };
            var filamentResponse = await _client.PostAsJsonAsync("/api/filaments", filament, _jsonOptions);
            var createdFilament = await filamentResponse.Content.ReadFromJsonAsync<Filament>(_jsonOptions);

            var sale = new Sale { Description = "Test Sale", FilamentId = createdFilament!.Id, MassGrams = 100 };
            await _client.PostAsJsonAsync("/api/sales", sale, _jsonOptions);

            // Act
            var response = await _client.DeleteAsync($"/api/filaments/{createdFilament.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task DeleteClient_WithAssociatedSale_ShouldFail()
        {
            // Arrange
            var client = new Client { Name = "Test Client", PhoneNumber = "123456789" };
            var clientResponse = await _client.PostAsJsonAsync("/api/clients", client, _jsonOptions);
            var createdClient = await clientResponse.Content.ReadFromJsonAsync<Client>(_jsonOptions);

            var filament = new Filament { Description = "Test Filament", Color = "Red", Price = 100, InitialMassGrams = 1000, RemainingMassGrams = 1000 };
            var filamentResponse = await _client.PostAsJsonAsync("/api/filaments", filament, _jsonOptions);
            var createdFilament = await filamentResponse.Content.ReadFromJsonAsync<Filament>(_jsonOptions);

            var sale = new Sale { Description = "Test Sale", ClientId = createdClient!.Id, FilamentId = createdFilament!.Id, MassGrams = 100 };
            await _client.PostAsJsonAsync("/api/sales", sale, _jsonOptions);

            // Act
            var response = await _client.DeleteAsync($"/api/clients/{createdClient.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task DeleteFilament_WithoutAssociatedSale_ShouldSucceed()
        {
            // Arrange
            var filament = new Filament { Description = "Test Filament", Color = "Red", Price = 100, InitialMassGrams = 1000, RemainingMassGrams = 1000 };
            var filamentResponse = await _client.PostAsJsonAsync("/api/filaments", filament, _jsonOptions);
            var createdFilament = await filamentResponse.Content.ReadFromJsonAsync<Filament>(_jsonOptions);

            // Act
            var response = await _client.DeleteAsync($"/api/filaments/{createdFilament!.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task DeleteClient_WithoutAssociatedSale_ShouldSucceed()
        {
            // Arrange
            var client = new Client { Name = "Test Client", PhoneNumber = "123456789" };
            var clientResponse = await _client.PostAsJsonAsync("/api/clients", client, _jsonOptions);
            var createdClient = await clientResponse.Content.ReadFromJsonAsync<Client>(_jsonOptions);

            // Act
            var response = await _client.DeleteAsync($"/api/clients/{createdClient!.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }
    }
}
