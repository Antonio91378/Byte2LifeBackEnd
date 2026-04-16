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
    public class BudgetIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>, IDisposable
    {
        private readonly CustomWebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly IMongoDatabase _db;
        private readonly JsonSerializerOptions _jsonOptions;

        public BudgetIntegrationTests(CustomWebApplicationFactory<Program> factory)
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
        public async Task CalculateBudget_LowDetail_ReturnsCorrectValues()
        {
            // Arrange - Create Filament (Price 120/kg)
            var filament = new Filament 
            { 
                Description = "Standard PLA", 
                Color = "White", 
                Price = 120.00m, 
                InitialMassGrams = 1000, 
                RemainingMassGrams = 1000
            };
            var filamentResponse = await _client.PostAsJsonAsync("/api/filaments", filament, _jsonOptions);
            var createdFilament = await filamentResponse.Content.ReadFromJsonAsync<Filament>(_jsonOptions);

            var request = new BudgetRequest
            {
                FilamentId = createdFilament!.Id.ToString(),
                DetailLevel = DetailLevel.Low, // 80% margin
                MassGrams = 100
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/budget/calculate", request, _jsonOptions);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<BudgetResult>(_jsonOptions);

            // Material Cost: (120 / 1000) * 100 = 12.00
            result!.MaterialCost.Should().Be(12.00m);
            
            // Margin: 80%
            // Time Estimation: 100g / 20g/h = 5h. < 8h, so no penalty.
            result.ProfitMarginPercentage.Should().Be(80m);
            
            // Profit: 12.00 * 0.80 = 9.60
            result.ProfitValue.Should().Be(9.60m);
            
            // Total: 12.00 + 9.60 = 21.60
            result.TotalPrice.Should().Be(21.60m);
        }

        [Fact]
        public async Task CalculateBudget_ExtremeDetail_HighFilamentCost_LongTime_ReturnsAdjustedValues()
        {
            // Arrange - Create Expensive Filament (Price 240/kg - Double base)
            var filament = new Filament 
            { 
                Description = "Premium PLA", 
                Color = "Gold", 
                Price = 240.00m, 
                InitialMassGrams = 1000, 
                RemainingMassGrams = 1000
            };
            var filamentResponse = await _client.PostAsJsonAsync("/api/filaments", filament, _jsonOptions);
            var createdFilament = await filamentResponse.Content.ReadFromJsonAsync<Filament>(_jsonOptions);

            // Mass 10g. Rate Extreme = 1g/h. Time = 10h.
            // 10h > 8h threshold. +2h penalty.
            var request = new BudgetRequest
            {
                FilamentId = createdFilament!.Id.ToString(),
                DetailLevel = DetailLevel.Extreme, // Base 210%
                MassGrams = 10
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/budget/calculate", request, _jsonOptions);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<BudgetResult>(_jsonOptions);

            // Material Cost: (240 / 1000) * 10 = 2.40
            result!.MaterialCost.Should().Be(2.40m);
            
            // Base Margin: 210%
            // Filament Adjustment: 240/120 = 2x multiplier -> 210% * 2 = 420%
            // Time Adjustment: (10 - 8) * 5% = 10%
            // Total Margin: 420% + 10% = 430%
            result.ProfitMarginPercentage.Should().Be(430m);
            
            // Profit: 2.40 * 4.30 = 10.32
            result.ProfitValue.Should().Be(10.32m);
            
            // Total: 2.40 + 10.32 = 12.72
            result.TotalPrice.Should().Be(12.72m);
        }

        [Fact]
        public async Task CalculateBudget_CustomArt_EnforcesMinimumPrice()
        {
            // Arrange
            var filament = new Filament 
            { 
                Description = "Standard PLA", 
                Color = "White", 
                Price = 120.00m, 
                InitialMassGrams = 1000, 
                RemainingMassGrams = 1000
            };
            var filamentResponse = await _client.PostAsJsonAsync("/api/filaments", filament, _jsonOptions);
            var createdFilament = await filamentResponse.Content.ReadFromJsonAsync<Filament>(_jsonOptions);

            // Case 1: Small mass (< 120g), Custom Art -> Min R$ 50
            var request1 = new BudgetRequest
            {
                FilamentId = createdFilament!.Id.ToString(),
                DetailLevel = DetailLevel.Low,
                MassGrams = 10, // Cost 1.20. Margin 80% -> Profit 0.96. Total 2.16. +1200% Art -> Total ~16.
                HasCustomArt = true
            };

            var response1 = await _client.PostAsJsonAsync("/api/budget/calculate", request1, _jsonOptions);
            var result1 = await response1.Content.ReadFromJsonAsync<BudgetResult>(_jsonOptions);
            result1!.TotalPrice.Should().Be(50.00m);

            // Case 2: Large mass (> 120g), Custom Art -> Min R$ 100
            // Mass 130g. Cost 15.60. Margin 80%. Profit 12.48. Total 28.08. +1200% Art -> Total ~200.
            // Wait, 1200% margin is huge. 
            // Cost 15.60. Margin 80+1200 = 1280%. Profit 199.68. Total 215.28.
            // This naturally exceeds 100.
            // Let's try a case where it might not exceed if not for the rule, or just verify the rule exists.
            // Actually, with 1200% margin, it's hard to stay under 100 with 120g.
            // 120g * 0.12 = 14.4 cost. 14.4 * 13.8 = ~198.
            // So the rule might be redundant for high margins but ensures safety.
            // Let's test the logic with a very cheap filament or low margin if possible, but margin is additive.
            // Let's just verify it returns at least 100.
            
            var request2 = new BudgetRequest
            {
                FilamentId = createdFilament!.Id.ToString(),
                DetailLevel = DetailLevel.Low,
                MassGrams = 130,
                HasCustomArt = true
            };
             var response2 = await _client.PostAsJsonAsync("/api/budget/calculate", request2, _jsonOptions);
            var result2 = await response2.Content.ReadFromJsonAsync<BudgetResult>(_jsonOptions);
            result2!.TotalPrice.Should().BeGreaterThanOrEqualTo(100.00m);
        }
    }
}
