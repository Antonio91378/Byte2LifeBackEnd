using Byte2Life.API.Services;
using LiteDB;
using Microsoft.AspNetCore.Mvc;

namespace Byte2Life.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DebugController : ControllerBase
    {
        private readonly LiteDatabase _database;
        private readonly IWebHostEnvironment _env;

        public DebugController(LiteDatabase database, IWebHostEnvironment env)
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

            var collectionNames = _database.GetCollectionNames().ToList();
            foreach (var name in collectionNames)
            {
                _database.DropCollection(name);
            }

            return Ok("Database reset successfully.");
        }
    }
}
