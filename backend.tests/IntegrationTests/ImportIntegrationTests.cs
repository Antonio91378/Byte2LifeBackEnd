using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AutoBogus;
using Bogus;
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
    public class ImportIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>, IDisposable
    {
        private readonly CustomWebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly IMongoDatabase _db;
        private readonly JsonSerializerOptions _jsonOptions;

        public ImportIntegrationTests(CustomWebApplicationFactory<Program> factory)
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

            // Ensure clean state
            _db.GetCollection<Sale>(MongoCollectionNames.Sales).DeleteMany(Builders<Sale>.Filter.Empty);
            _db.GetCollection<Filament>(MongoCollectionNames.Filaments).DeleteMany(Builders<Filament>.Filter.Empty);
            _db.GetCollection<Client>(MongoCollectionNames.Clients).DeleteMany(Builders<Client>.Filter.Empty);
        }

        public void Dispose()
        {
            _db.GetCollection<Sale>(MongoCollectionNames.Sales).DeleteMany(Builders<Sale>.Filter.Empty);
            _db.GetCollection<Filament>(MongoCollectionNames.Filaments).DeleteMany(Builders<Filament>.Filter.Empty);
            _db.GetCollection<Client>(MongoCollectionNames.Clients).DeleteMany(Builders<Client>.Filter.Empty);
        }

        private string GenerateCsvLine(bool isValid = true)
        {
            var faker = new Faker();
            
            if (!isValid)
            {
                return "Invalid;CSV;Line";
            }

            // Columns based on ImportController:
            // 0: Description
            // 1: LinkFilament
            // 2: PriceFilament
            // 3: FilamentDesc
            // 4: FilamentColor (NEW)
            // 5: LinkProduct
            // 6: PrintQuality
            // 7: Mass
            // 8: Cost
            // 9: SaleValue
            // 10: Profit
            // 11: ProfitPercent
            // 12: ClientSex
            // 13: Category
            // 14: ClientNumber
            // 15: ClientName (NEW)
            // 16: PrintConcluded
            // 17: ProductDelivered
            // 18: ProductPaid
            // 19: TimeDesignPrint
            // 20: PrintStatus (NEW)
            // 21: SaleDate (NEW)

            var description = faker.Commerce.ProductName();
            var linkFilament = faker.Internet.Url();
            var priceFilament = faker.Random.Decimal(50, 300).ToString("F2");
            var filamentDesc = faker.Commerce.Color() + " PLA";
            var filamentColor = faker.Commerce.Color();
            var linkProduct = faker.Internet.Url();
            var printQuality = faker.PickRandom(new[] { "High", "Medium", "Low" });
            var mass = faker.Random.Double(10, 500).ToString("F2");
            var cost = faker.Random.Decimal(5, 50).ToString("F2");
            var saleValue = faker.Random.Decimal(20, 100).ToString("F2");
            var profit = faker.Random.Decimal(10, 50).ToString("F2");
            var profitPercent = "50%";
            var clientSex = faker.PickRandom(new[] { "M", "F" });
            var category = faker.Commerce.Department();
            var clientNumber = faker.Phone.PhoneNumber("###########");
            var clientName = faker.Name.FullName();
            var printConcluded = faker.PickRandom(new[] { "S", "N" });
            var productDelivered = faker.PickRandom(new[] { "S", "N" });
            var productPaid = faker.PickRandom(new[] { "S", "N" });
            var timeDesignPrint = "2h 30m";
            var printStatus = "Pending";
            var saleDate = faker.Date.Past().ToString("dd/MM/yyyy");

            return $"{description};{linkFilament};{priceFilament};{filamentDesc};{filamentColor};{linkProduct};{printQuality};{mass};{cost};{saleValue};{profit};{profitPercent};{clientSex};{category};{clientNumber};{clientName};{printConcluded};{productDelivered};{productPaid};{timeDesignPrint};{printStatus};{saleDate}";
        }

        [Fact]
        public async Task Import_ValidCsv_CreatesEntities()
        {
            // Arrange
            var header = "Description;LinkFilament;PriceFilament;FilamentDesc;FilamentColor;LinkProduct;PrintQuality;Mass;Cost;SaleValue;Profit;ProfitPercent;ClientSex;Category;ClientNumber;ClientName;PrintConcluded;ProductDelivered;ProductPaid;TimeDesignPrint;PrintStatus;SaleDate";
            var line1 = GenerateCsvLine();
            var line2 = GenerateCsvLine();
            var csvContent = $"{header}\n{line1}\n{line2}";

            using var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csvContent));
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
            content.Add(fileContent, "file", "import.csv");

            // Act
            var response = await _client.PostAsync("/api/import", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            // Verify Sales created
            var sales = _db.GetCollection<Sale>(MongoCollectionNames.Sales).Find(Builders<Sale>.Filter.Empty).ToList();
            sales.Should().HaveCount(2);

            // Verify Filaments created (assuming different filaments generated)
            var filaments = _db.GetCollection<Filament>(MongoCollectionNames.Filaments).Find(Builders<Filament>.Filter.Empty).ToList();
            filaments.Should().NotBeEmpty();

            // Verify Clients created
            var clients = _db.GetCollection<Client>(MongoCollectionNames.Clients).Find(Builders<Client>.Filter.Empty).ToList();
            clients.Should().NotBeEmpty();
        }

        [Fact]
        public async Task Import_InvalidColumns_ReturnsErrors()
        {
            // Arrange
            var header = "Description;LinkFilament";
            var line1 = GenerateCsvLine(isValid: false);
            var csvContent = $"{header}\n{line1}";

            using var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csvContent));
            content.Add(fileContent, "file", "invalid.csv");

            // Act
            var response = await _client.PostAsync("/api/import", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK); // Controller returns OK with error list
            var result = await response.Content.ReadFromJsonAsync<ImportResult>(_jsonOptions);
            
            result.Should().NotBeNull();
            result!.FailureCount.Should().Be(1);
            result.Errors.Should().ContainMatch("*Insufficient columns*");
        }

        [Fact]
        public async Task Import_EmptyFile_ReturnsBadRequest()
        {
            // Arrange
            using var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(Array.Empty<byte>());
            content.Add(fileContent, "file", "empty.csv");

            // Act
            var response = await _client.PostAsync("/api/import", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Import_CsvWithDotDecimalSeparator_ParsesCorrectly()
        {
            // Arrange
            var header = "Description;LinkFilament;PriceFilament;FilamentDesc;FilamentColor;LinkProduct;PrintQuality;Mass;Cost;SaleValue;Profit;ProfitPercent;ClientSex;Category;ClientNumber;ClientName;PrintConcluded;ProductDelivered;ProductPaid;TimeDesignPrint;PrintStatus;SaleDate";
            
            // Construct a line with specific dot-separated values
            // PriceFilament: 50.00
            // Mass: 10.5
            // Cost: 5.25
            // SaleValue: 100.50
            // Profit: 45.25
            var line = "TestItem;http://fil.com;50.00;TestFilament;Red;http://prod.com;High;10.5;5.25;100.50;45.25;50%;M;TestCat;123456789;TestClient;S;S;S;1h;Pending;2023-10-25";
            
            var csvContent = $"{header}\n{line}";

            using var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csvContent));
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
            content.Add(fileContent, "file", "dot_separator.csv");

            // Act
            var response = await _client.PostAsync("/api/import", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var result = await response.Content.ReadFromJsonAsync<ImportResult>(_jsonOptions);
            result.Should().NotBeNull();
            result!.SuccessCount.Should().Be(1);
            result.Errors.Should().BeEmpty();

            // Verify values in DB
            var sale = _db.GetCollection<Sale>(MongoCollectionNames.Sales).Find(x => x.Description == "TestItem").FirstOrDefault();
            sale.Should().NotBeNull();
            sale.MassGrams.Should().Be(10.5);
            sale.Cost.Should().Be(5.25m);
            sale.SaleValue.Should().Be(100.50m);
            sale.Profit.Should().Be(45.25m);

            var filament = _db.GetCollection<Filament>(MongoCollectionNames.Filaments)
                .Find(MongoId.FilterById<Filament>(sale.FilamentId!.Value))
                .FirstOrDefault();
            filament.Should().NotBeNull();
            filament.Price.Should().Be(50.00m);
        }

        [Fact]
        public async Task Import_CsvWithCommaDecimals_ShouldSucceed()
        {
            // Arrange
            var header = "Description;LinkFilament;PriceFilament;FilamentDesc;FilamentColor;LinkProduct;PrintQuality;Mass;Cost;SaleValue;Profit;ProfitPercent;ClientSex;Category;ClientNumber;ClientName;PrintConcluded;ProductDelivered;ProductPaid;TimeDesignPrint;PrintStatus;SaleDate";
            // Line with commas for decimals: "12,50"
            // Mass: "10,5" -> 10.5
            // Cost: "5,00" -> 5.00
            // SaleValue: "20,00" -> 20.00
            var line = "TestItemComma;http://fil.com;100,00;PLA Red;Red;http://prod.com;High;10,5;5,00;20,00;15,00;300%;M;Decor;123456789;John Doe;S;S;S;2h;Concluded;01/01/2023";
            
            var csvContent = $"{header}\n{line}";
            var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csvContent));
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
            content.Add(fileContent, "file", "comma_separator.csv");

            // Act
            var response = await _client.PostAsync("/api/import", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ImportResult>(_jsonOptions);
            result!.SuccessCount.Should().Be(1);
            result.FailureCount.Should().Be(0);

            var sale = _db.GetCollection<Sale>(MongoCollectionNames.Sales).Find(x => x.Description == "TestItemComma").FirstOrDefault();
            sale.Should().NotBeNull();
            sale.MassGrams.Should().Be(10.5);
            sale.Cost.Should().Be(5.00m);
            sale.SaleValue.Should().Be(20.00m);
        }
    }

    public class ImportResult
    {
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
