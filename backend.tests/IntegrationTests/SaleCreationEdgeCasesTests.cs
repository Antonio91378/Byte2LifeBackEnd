using System.Net;
using System.Text;
using System.Text.Json;
using Byte2Life.API.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Byte2Life.API.Tests.IntegrationTests
{
    public class SaleCreationEdgeCasesTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public SaleCreationEdgeCasesTests(CustomWebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task CreateSale_WithEmptyStringIds_ShouldNotFail()
        {
            // Arrange
            var json = @"{
                ""description"": ""Test Sale"",
                ""productLink"": ""http://example.com"",
                ""printQuality"": ""Standard"",
                ""massGrams"": 100,
                ""cost"": 10,
                ""saleValue"": 20,
                ""profit"": 10,
                ""profitPercentage"": ""100%"",
                ""designPrintTime"": ""1h"",
                ""isPrintConcluded"": false,
                ""isDelivered"": false,
                ""isPaid"": false,
                ""filamentId"": """",
                ""clientId"": """"
            }";
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/sales", content);

            // Assert
            if (response.StatusCode != HttpStatusCode.Created)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed with status {response.StatusCode}: {error}");
            }
            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }
    }
}
