using MongoDB.Bson;
using MongoDB.Driver;

namespace Byte2Life.API.Persistence
{
    public static class MongoId
    {
        public static ObjectId New() => ObjectId.GenerateNewId();

        public static ObjectId Parse(string id) => ObjectId.Parse(id);

        public static FilterDefinition<TDocument> FilterById<TDocument>(string id) =>
            Builders<TDocument>.Filter.Eq("_id", Parse(id));

        public static FilterDefinition<TDocument> FilterById<TDocument>(ObjectId id) =>
            Builders<TDocument>.Filter.Eq("_id", id);
    }
}