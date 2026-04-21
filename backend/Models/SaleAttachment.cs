using MongoDB.Bson.Serialization.Attributes;

namespace Byte2Life.API.Models
{
    [BsonIgnoreExtraElements]
    public class SaleAttachment
    {
        public string StorageId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/octet-stream";
        public long SizeBytes { get; set; }
        public string Category { get; set; } = SaleAttachmentCategories.ComplementaryFile;
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }

    public static class SaleAttachmentCategories
    {
        public const string ProductImage = "product-image";
        public const string ComplementaryImage = "complementary-image";
        public const string ComplementaryFile = "complementary-file";

        public static bool IsImage(string? category)
        {
            return string.Equals(category, ProductImage, StringComparison.Ordinal) ||
                   string.Equals(category, ComplementaryImage, StringComparison.Ordinal);
        }

        public static bool IsValid(string? category)
        {
            return string.Equals(category, ProductImage, StringComparison.Ordinal) ||
                   string.Equals(category, ComplementaryImage, StringComparison.Ordinal) ||
                   string.Equals(category, ComplementaryFile, StringComparison.Ordinal);
        }
    }
}