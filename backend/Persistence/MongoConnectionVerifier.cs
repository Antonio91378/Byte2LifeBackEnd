using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Byte2Life.API.Persistence
{
    public static class MongoConnectionVerifier
    {
        public static async Task PingAsync(IMongoDatabase database)
        {
            await database.RunCommandAsync((Command<BsonDocument>)"{ ping: 1 }");
        }

        public static async Task VerifyAsync(IMongoDatabase database, ILogger logger)
        {
            await PingAsync(database);
            logger.LogInformation("MongoDB connection verified for database {DatabaseName}", database.DatabaseNamespace.DatabaseName);
        }
    }
}