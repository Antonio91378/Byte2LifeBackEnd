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
    public class ClientIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>, IDisposable
    {
        private readonly CustomWebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly LiteDatabase _db;
        private readonly JsonSerializerOptions _jsonOptions;

        public ClientIntegrationTests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
            
            // Get access to the database to clean it up
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
            // Clean up the database after each test
            var col = _db.GetCollection<Client>("clients");
            col.DeleteAll();
        }

        [Fact]
        public async Task GetClients_ReturnsEmptyList_WhenNoClientsExist()
        {
            // Act
            var response = await _client.GetAsync("/api/clients");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var clients = await response.Content.ReadFromJsonAsync<List<Client>>(_jsonOptions);
            clients.Should().BeEmpty();
        }

        [Fact]
        public async Task CreateClient_ReturnsCreatedClient()
        {
            // Arrange
            var newClient = new Client { Name = "Test Client", Category = "Test Category", PhoneNumber = "123456789" };

            // Act
            var response = await _client.PostAsJsonAsync("/api/clients", newClient, _jsonOptions);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var createdClient = await response.Content.ReadFromJsonAsync<Client>(_jsonOptions);
            createdClient.Should().NotBeNull();
            createdClient!.Name.Should().Be(newClient.Name);
            createdClient.Id.Should().NotBeNull();
        }
    }
}
