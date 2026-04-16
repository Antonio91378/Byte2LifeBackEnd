using System.Text.Json;
using System.Text.Json.Serialization;
using MongoDB.Bson;

namespace Byte2Life.API.Converters
{
    public class ObjectIdConverter : JsonConverter<ObjectId?>
    {
        public override ObjectId? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            var value = reader.GetString();
            if (string.IsNullOrWhiteSpace(value)) return null;
            return ObjectId.Parse(value);
        }

        public override void Write(Utf8JsonWriter writer, ObjectId? value, JsonSerializerOptions options)
        {
            if (!value.HasValue || value.Value == ObjectId.Empty)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteStringValue(value.Value.ToString());
            }
        }
    }
}
