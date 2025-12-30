using System.Net;
using System.Net.Http.Headers;
using Byte2Life.API.Tests;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Byte2Life.API.IntegrationTests
{
    public class StockUploadTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory<Program> _factory;

        public StockUploadTests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Theory]
        [InlineData("test-image.jpg", "image/jpeg")]
        [InlineData("test-image.png", "image/png")]
        [InlineData("test-image.gif", "image/gif")]
        public async Task UploadPhoto_WithValidImage_ReturnsOkAndUrl(string fileName, string contentType)
        {
            // Arrange
            var content = new MultipartFormDataContent();
            // Create dummy file content
            var fileContent = new ByteArrayContent(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }); 
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
            content.Add(fileContent, "file", fileName);

            // Act
            var response = await _client.PostAsync("/api/stock/upload", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var responseString = await response.Content.ReadAsStringAsync();
            responseString.Should().Contain("url");
            responseString.Should().Contain("/uploads/");
            responseString.Should().Contain(Path.GetExtension(fileName));
        }

        [Fact]
        public async Task UploadPhoto_NoFile_ReturnsBadRequest()
        {
            // Arrange
            var content = new MultipartFormDataContent();

            // Act
            var response = await _client.PostAsync("/api/stock/upload", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }
}
