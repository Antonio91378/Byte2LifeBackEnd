using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Byte2Life.API.Converters;
using Byte2Life.API.Models;
using Byte2Life.API.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Xunit;

namespace Byte2Life.API.Tests.IntegrationTests
{
    public class PaintingScheduleIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>, IDisposable
    {
        private readonly HttpClient _client;
        private readonly IMongoDatabase _db;
        private readonly JsonSerializerOptions _jsonOptions;

        public PaintingScheduleIntegrationTests(CustomWebApplicationFactory<Program> factory)
        {
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
        }

        [Fact]
        public async Task PatchPaintSchedule_PersistsAndReturnsFromPaintingEndpoint()
        {
            var printStart = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
            var paintStart = new DateTime(2026, 1, 1, 13, 0, 0, DateTimeKind.Utc);
            var sale = new Sale
            {
                Description = "Paint Patch",
                PrintStatus = "InQueue",
                PrintStartConfirmedAt = printStart,
                PrintTimeHours = 2,
                HasPainting = true
            };

            var createResponse = await _client.PostAsJsonAsync("/api/sales", sale, _jsonOptions);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var createdSale = await createResponse.Content.ReadFromJsonAsync<Sale>(_jsonOptions);

            var payload = new
            {
                paintStartConfirmedAt = paintStart,
                paintTimeHours = 1.5,
                paintResponsible = "Ana"
            };
            var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/sales/{createdSale!.Id}/paint-schedule")
            {
                Content = JsonContent.Create(payload, options: _jsonOptions)
            };

            var patchResponse = await _client.SendAsync(request);
            patchResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var getResponse = await _client.GetAsync($"/api/sales/{createdSale.Id}");
            var updatedSale = await getResponse.Content.ReadFromJsonAsync<Sale>(_jsonOptions);

            updatedSale!.PaintStartConfirmedAt.Should().NotBeNull();
            updatedSale.PaintStartConfirmedAt!.Value.ToUniversalTime().Should().Be(paintStart);
            updatedSale.PaintTimeHours.Should().Be(1.5);
            updatedSale.PaintResponsible.Should().Be("Ana");
            updatedSale.HasPainting.Should().BeTrue();

            var paintingSales = await _client.GetFromJsonAsync<List<Sale>>("/api/sales/painting", _jsonOptions);
            paintingSales.Should().Contain(s => s.Id == createdSale.Id && s.PaintStartConfirmedAt != null);
        }

        [Fact]
        public async Task PatchPaintSchedule_BeforePrintEnd_ReturnsBadRequest()
        {
            var printStart = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
            var paintStart = new DateTime(2026, 1, 1, 11, 0, 0, DateTimeKind.Utc);
            var sale = new Sale
            {
                Description = "Paint Validation",
                PrintStatus = "InQueue",
                PrintStartConfirmedAt = printStart,
                PrintTimeHours = 2,
                HasPainting = true
            };

            var createResponse = await _client.PostAsJsonAsync("/api/sales", sale, _jsonOptions);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var createdSale = await createResponse.Content.ReadFromJsonAsync<Sale>(_jsonOptions);

            var payload = new
            {
                paintStartConfirmedAt = paintStart,
                paintTimeHours = 1.0
            };
            var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/sales/{createdSale!.Id}/paint-schedule")
            {
                Content = JsonContent.Create(payload, options: _jsonOptions)
            };

            var patchResponse = await _client.SendAsync(request);
            patchResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task PutSale_WithPaintingSchedule_PersistsToPaintingEndpoint()
        {
            var sale = new Sale
            {
                Description = "Paint Put",
                PrintStatus = "InQueue",
                HasPainting = true
            };

            var createResponse = await _client.PostAsJsonAsync("/api/sales", sale, _jsonOptions);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var createdSale = await createResponse.Content.ReadFromJsonAsync<Sale>(_jsonOptions);

            var printStart = new DateTime(2026, 2, 1, 9, 0, 0, DateTimeKind.Utc);
            var paintStart = new DateTime(2026, 2, 1, 11, 30, 0, DateTimeKind.Utc);
            createdSale!.PrintStartConfirmedAt = printStart;
            createdSale.PrintTimeHours = 2;
            createdSale.PaintStartConfirmedAt = paintStart;
            createdSale.PaintTimeHours = 1;
            createdSale.PaintResponsible = "Bruno";

            var updateResponse = await _client.PutAsJsonAsync($"/api/sales/{createdSale.Id}", createdSale, _jsonOptions);
            updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var paintingSales = await _client.GetFromJsonAsync<List<Sale>>("/api/sales/painting", _jsonOptions);
            paintingSales.Should().Contain(s => s.Id == createdSale.Id && s.PaintStartConfirmedAt != null);
        }
    }
}
