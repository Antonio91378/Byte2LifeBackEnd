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
    public class DashboardIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>, IDisposable
    {
        private readonly CustomWebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly IMongoDatabase _db;
        private readonly JsonSerializerOptions _jsonOptions;

        public DashboardIntegrationTests(CustomWebApplicationFactory<Program> factory)
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
        }

        private async Task<Filament> CreateFilamentAsync()
        {
            var filament = new Filament 
            { 
                Description = "Test Filament", 
                InitialMassGrams = 1000, 
                RemainingMassGrams = 1000 
            };
            var response = await _client.PostAsJsonAsync("/api/filaments", filament, _jsonOptions);
            return await response.Content.ReadFromJsonAsync<Filament>(_jsonOptions);
        }

        [Fact]
        public async Task SetStatusInProgress_Succeeds_WhenNoOtherInProgress()
        {
            // Arrange
            var filament = await CreateFilamentAsync();
            var sale = new Sale { Description = "Job 1", FilamentId = filament.Id, PrintStatus = "Pending" };
            var createResponse = await _client.PostAsJsonAsync("/api/sales", sale, _jsonOptions);
            var createdSale = await createResponse.Content.ReadFromJsonAsync<Sale>(_jsonOptions);

            createdSale!.PrintStatus = "InProgress";

            // Act
            var updateResponse = await _client.PutAsJsonAsync($"/api/sales/{createdSale.Id}", createdSale, _jsonOptions);

            // Assert
            updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
            
            var getResponse = await _client.GetAsync($"/api/sales/{createdSale.Id}");
            var updatedSale = await getResponse.Content.ReadFromJsonAsync<Sale>(_jsonOptions);
            updatedSale!.PrintStatus.Should().Be("InProgress");
        }

        [Fact]
        public async Task SetStatusInProgress_Fails_WhenAnotherInProgress()
        {
            // Arrange
            var filament = await CreateFilamentAsync();
            
            // Sale 1: InProgress
            var sale1 = new Sale { Description = "Job 1", FilamentId = filament.Id, PrintStatus = "InProgress" };
            await _client.PostAsJsonAsync("/api/sales", sale1, _jsonOptions);

            // Sale 2: Try to set InProgress
            var sale2 = new Sale { Description = "Job 2", FilamentId = filament.Id, PrintStatus = "Pending" };
            var createResponse2 = await _client.PostAsJsonAsync("/api/sales", sale2, _jsonOptions);
            var createdSale2 = await createResponse2.Content.ReadFromJsonAsync<Sale>(_jsonOptions);

            createdSale2!.PrintStatus = "InProgress";

            // Act
            var updateResponse = await _client.PutAsJsonAsync($"/api/sales/{createdSale2.Id}", createdSale2, _jsonOptions);

            // Assert
            updateResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var error = await updateResponse.Content.ReadAsStringAsync();
            error.Should().Contain("Only one sale can be InProgress");
        }

        [Fact]
        public async Task GetQueue_ReturnsSalesSortedByPriority()
        {
            // Arrange
            var filament = await CreateFilamentAsync();
            
            var saleLow = new Sale { Description = "Low Prio", FilamentId = filament.Id, PrintStatus = "InQueue", Priority = 1 };
            var saleHigh = new Sale { Description = "High Prio", FilamentId = filament.Id, PrintStatus = "InQueue", Priority = 10 };
            var saleMed = new Sale { Description = "Med Prio", FilamentId = filament.Id, PrintStatus = "InQueue", Priority = 5 };

            await _client.PostAsJsonAsync("/api/sales", saleLow, _jsonOptions);
            await _client.PostAsJsonAsync("/api/sales", saleHigh, _jsonOptions);
            await _client.PostAsJsonAsync("/api/sales", saleMed, _jsonOptions);

            // Act
            var response = await _client.GetAsync("/api/sales/queue");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var queue = await response.Content.ReadFromJsonAsync<List<Sale>>(_jsonOptions);
            
            queue.Should().HaveCount(3);
            queue![0].Priority.Should().Be(10);
            queue![1].Priority.Should().Be(5);
            queue![2].Priority.Should().Be(1);
        }

        [Fact]
        public async Task GetQueue_DoesNotReturnInactiveSales()
        {
            // Arrange
            var filament = await CreateFilamentAsync();

            var activeSale = new Sale
            {
                Description = "Active Queue",
                FilamentId = filament.Id,
                PrintStatus = "InQueue",
                Priority = 3,
                IsActive = true
            };
            var inactiveSale = new Sale
            {
                Description = "Inactive Queue",
                FilamentId = filament.Id,
                PrintStatus = "InQueue",
                Priority = 9,
                IsActive = false
            };

            await _client.PostAsJsonAsync("/api/sales", activeSale, _jsonOptions);
            await _client.PostAsJsonAsync("/api/sales", inactiveSale, _jsonOptions);

            // Act
            var response = await _client.GetAsync("/api/sales/queue");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var queue = await response.Content.ReadFromJsonAsync<List<Sale>>(_jsonOptions);

            queue.Should().HaveCount(1);
            queue![0].Description.Should().Be("Active Queue");
        }
        
        [Fact]
        public async Task GetCurrentPrint_ReturnsInProgressSale()
        {
            // Arrange
            var filament = await CreateFilamentAsync();
            var sale = new Sale { Description = "Current Job", FilamentId = filament.Id, PrintStatus = "InProgress" };
            await _client.PostAsJsonAsync("/api/sales", sale, _jsonOptions);

            // Act
            var response = await _client.GetAsync("/api/sales/current");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var current = await response.Content.ReadFromJsonAsync<Sale>(_jsonOptions);
            current.Should().NotBeNull();
            current!.Description.Should().Be("Current Job");
        }

        [Fact]
        public async Task GetCurrentPrint_ReturnsNoContent_WhenOnlyInactiveCurrentSaleExists()
        {
            // Arrange
            var filament = await CreateFilamentAsync();
            var sale = new Sale
            {
                Description = "Dormant Current Job",
                FilamentId = filament.Id,
                PrintStatus = "InProgress",
                IsActive = false
            };
            await _client.PostAsJsonAsync("/api/sales", sale, _jsonOptions);

            // Act
            var response = await _client.GetAsync("/api/sales/current");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task SetStatusInProgress_Succeeds_WhenOtherInProgressIsInactive()
        {
            // Arrange
            var filament = await CreateFilamentAsync();

            var inactiveCurrentSale = new Sale
            {
                Description = "Dormant Current Job",
                FilamentId = filament.Id,
                PrintStatus = "InProgress",
                IsActive = false
            };
            await _client.PostAsJsonAsync("/api/sales", inactiveCurrentSale, _jsonOptions);

            var nextSale = new Sale
            {
                Description = "Active Job",
                FilamentId = filament.Id,
                PrintStatus = "Pending",
                IsActive = true
            };
            var createResponse = await _client.PostAsJsonAsync("/api/sales", nextSale, _jsonOptions);
            var createdSale = await createResponse.Content.ReadFromJsonAsync<Sale>(_jsonOptions);

            createdSale.Should().NotBeNull();
            createdSale!.PrintStatus = "InProgress";

            // Act
            var updateResponse = await _client.PutAsJsonAsync($"/api/sales/{createdSale.Id}", createdSale, _jsonOptions);

            // Assert
            updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var currentPrintResponse = await _client.GetAsync("/api/sales/current");
            currentPrintResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var currentPrint = await currentPrintResponse.Content.ReadFromJsonAsync<Sale>(_jsonOptions);

            currentPrint.Should().NotBeNull();
            currentPrint!.Description.Should().Be("Active Job");
        }

        [Fact]
        public async Task FailedPrintOccurrence_PersistsIncident_KeepsSaleAsCurrentPrint_AndDiscountsWasteFromFilament()
        {
            // Arrange
            var filament = await CreateFilamentAsync();
            var sale = new Sale
            {
                Description = "Job With Failure",
                FilamentId = filament.Id,
                PrintStatus = "InProgress",
                MassGrams = 100,
                PrintTimeHours = 2
            };

            var createResponse = await _client.PostAsJsonAsync("/api/sales", sale, _jsonOptions);
            createResponse.EnsureSuccessStatusCode();
            var createdSale = await createResponse.Content.ReadFromJsonAsync<Sale>(_jsonOptions);

            createdSale.Should().NotBeNull();

            createdSale!.PrintStatus = "Staged";
            createdSale.PrintStartedAt = null;
            createdSale.ErrorReason = "Peça soltou da mesa";
            createdSale.WastedFilamentGrams = 25;
            createdSale.Incidents = new List<PrintIncident>
            {
                new()
                {
                    Timestamp = DateTime.UtcNow,
                    Reason = "Other",
                    Comment = "Peça soltou da mesa",
                    WastedFilamentGrams = 25
                }
            };

            // Act
            var updateResponse = await _client.PutAsJsonAsync($"/api/sales/{createdSale.Id}", createdSale, _jsonOptions);

            // Assert - status/current print/incident persistence
            updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var getSaleResponse = await _client.GetAsync($"/api/sales/{createdSale.Id}");
            getSaleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var updatedSale = await getSaleResponse.Content.ReadFromJsonAsync<Sale>(_jsonOptions);

            updatedSale.Should().NotBeNull();
            updatedSale!.PrintStatus.Should().Be("Staged");
            updatedSale.Incidents.Should().ContainSingle();
            updatedSale.Incidents![0].Comment.Should().Be("Peça soltou da mesa");
            updatedSale.Incidents[0].WastedFilamentGrams.Should().Be(25);
            updatedSale.WastedFilamentGrams.Should().Be(25);

            var currentPrintResponse = await _client.GetAsync("/api/sales/current");
            currentPrintResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var currentPrint = await currentPrintResponse.Content.ReadFromJsonAsync<Sale>(_jsonOptions);

            currentPrint.Should().NotBeNull();
            currentPrint!.Id.Should().Be(updatedSale.Id);
            currentPrint.PrintStatus.Should().Be("Staged");

            // Assert - filament mass includes both planned usage and wasted grams
            var filamentResponse = await _client.GetAsync($"/api/filaments/{filament.Id}");
            filamentResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var updatedFilament = await filamentResponse.Content.ReadFromJsonAsync<Filament>(_jsonOptions);

            updatedFilament.Should().NotBeNull();
            updatedFilament!.RemainingMassGrams.Should().Be(875);
        }
    }
}
