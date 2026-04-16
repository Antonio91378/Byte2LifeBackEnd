using System.Net.Http.Json;
using Byte2Life.API.Models;
using Byte2Life.API.Tests;
using FluentAssertions;
using Xunit;

namespace Byte2Life.API.IntegrationTests
{
    public class StockFlowTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public StockFlowTests(CustomWebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task CreateStockItem_WithUploadedImage_ShouldSucceed()
        {
            // 1. Create a Filament
            var filament = new Filament
            {
                Description = "Test Filament",
                Price = 100.0m,
                InitialMassGrams = 1000,
                RemainingMassGrams = 1000,
                Color = "Red"
            };

            var filamentResponse = await _client.PostAsJsonAsync("/api/filaments", filament);
            filamentResponse.EnsureSuccessStatusCode();
            var createdFilament = await filamentResponse.Content.ReadFromJsonAsync<FilamentDto>();
            createdFilament.Should().NotBeNull();

            // 2. Upload an Image
            var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }); // Fake JPG header
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            content.Add(fileContent, "file", "test-image.jpg");

            var uploadResponse = await _client.PostAsync("/api/stock/upload", content);
            uploadResponse.EnsureSuccessStatusCode();
            var uploadResult = await uploadResponse.Content.ReadFromJsonAsync<UploadResult>();
            uploadResult.Should().NotBeNull();
            var imageUrl = uploadResult!.Url;

            // 3. Create Stock Item
            var stockItem = new StockItemDto
            {
                Description = "Test Stock Item",
                FilamentId = createdFilament!.Id,
                PrintTime = "2h 30m",
                WeightGrams = 150.5,
                Cost = 15.05,
                Photos = new List<string> { imageUrl }
            };

            var stockResponse = await _client.PostAsJsonAsync("/api/stock", stockItem);
            
            // Debugging info if it fails
            if (!stockResponse.IsSuccessStatusCode)
            {
                var errorContent = await stockResponse.Content.ReadAsStringAsync();
                throw new Exception($"Failed to create stock item. Status: {stockResponse.StatusCode}, Content: {errorContent}");
            }

            stockResponse.EnsureSuccessStatusCode();
            var createdStock = await stockResponse.Content.ReadFromJsonAsync<StockItemDto>();
            
            createdStock.Should().NotBeNull();
            createdStock!.Description.Should().Be("Test Stock Item");
            createdStock.Photos.Should().Contain(imageUrl);
        }

        private class UploadResult
        {
            public string Url { get; set; } = string.Empty;
        }

        private class FilamentDto
        {
            public string Id { get; set; }
            public string Description { get; set; }
            public decimal Price { get; set; }
        }

        private class StockItemDto
        {
            public string Id { get; set; }
            public string Description { get; set; }
            public string FilamentId { get; set; }
            public string PrintTime { get; set; }
            public double WeightGrams { get; set; }
            public double Cost { get; set; }
            public List<string> Photos { get; set; }
        }
    }
}
