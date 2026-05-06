using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using System.IO;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Byte2Life.API.Tests
{
    public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
    {
        private readonly string _databaseName;

        public CustomWebApplicationFactory()
        {
            _databaseName = $"btl_{Guid.NewGuid():N}";
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                var projectDir = Directory.GetCurrentDirectory();
                var configPath = Path.Combine(projectDir, "appsettings.Test.json");

                var connectionString = Environment.GetEnvironmentVariable("MongoDBSettings__ConnectionString");
                var overrides = new Dictionary<string, string?>
                {
                    ["MongoDBSettings:DatabaseName"] = _databaseName
                };

                if (!string.IsNullOrWhiteSpace(connectionString))
                {
                    overrides["MongoDBSettings:ConnectionString"] = connectionString;
                }

                config.AddJsonFile(configPath, optional: true);
                config.AddInMemoryCollection(overrides);
            });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    using var scope = Services.CreateScope();
                    var client = scope.ServiceProvider.GetRequiredService<IMongoClient>();
                    client.DropDatabase(_databaseName);
                }
                catch { /* Ignore cleanup errors */ }
            }

            base.Dispose(disposing);
        }
    }
}
