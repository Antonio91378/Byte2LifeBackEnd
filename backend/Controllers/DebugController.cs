using Byte2Life.API.Services;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Byte2Life.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DebugController : ControllerBase
    {
        private readonly IMongoDatabase _database;
        private readonly IWebHostEnvironment _env;

        public DebugController(IMongoDatabase database, IWebHostEnvironment env)
        {
            _database = database;
            _env = env;
        }

        [HttpDelete("reset-database")]
        public IActionResult ResetDatabase()
        {
            if (!_env.IsDevelopment())
            {
                return Forbid("This endpoint is only available in development environment.");
            }

            var collectionNames = _database.ListCollectionNames().ToList();
            foreach (var name in collectionNames)
            {
                _database.DropCollection(name);
            }

            return Ok("Database reset successfully.");
        }
    }
}
