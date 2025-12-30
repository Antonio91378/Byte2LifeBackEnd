using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using LiteDB;
using System.IO;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Byte2Life.API.Tests
{
    public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
    {
        private readonly string _dbPath;

        public CustomWebApplicationFactory()
        {
            _dbPath = $"Byte2Life_Test_{Guid.NewGuid()}.db";
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                var projectDir = Directory.GetCurrentDirectory();
                var configPath = Path.Combine(projectDir, "appsettings.Test.json");

                config.AddJsonFile(configPath, optional: true);
            });

            builder.ConfigureServices(services =>
            {
                // Remove the existing LiteDatabase registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(LiteDatabase));

                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add LiteDatabase with Unique Test Connection String
                services.AddSingleton<LiteDatabase>(sp =>
                {
                    return new LiteDatabase($"Filename={_dbPath};Connection=Shared");
                });
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                try
                {
                    if (File.Exists(_dbPath))
                    {
                        File.Delete(_dbPath);
                    }
                }
                catch { /* Ignore cleanup errors */ }
            }
        }
    }
}
