using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Byte2Life.API.Models
{
    [BsonIgnoreExtraElements]
    public class Investment
    {
        [BsonId]
        public ObjectId? Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        public string Category { get; set; } = "Geral";
    }
}
