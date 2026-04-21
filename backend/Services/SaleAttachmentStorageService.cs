using Byte2Life.API.Models;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace Byte2Life.API.Services
{
    public class SaleAttachmentStorageService : ISaleAttachmentStorageService
    {
        private const long MaxImageBytes = 2 * 1024 * 1024;
        private const long MaxFileBytes = 8 * 1024 * 1024;

        private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp",
            ".gif"
        };

        private static readonly HashSet<string> AllowedFileExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".3mf",
            ".stl",
            ".obj",
            ".zip",
            ".pdf",
            ".txt",
            ".csv",
            ".json",
            ".md"
        };

        private readonly GridFSBucket _bucket;

        public SaleAttachmentStorageService(IMongoDatabase database)
        {
            _bucket = new GridFSBucket(database, new GridFSBucketOptions
            {
                BucketName = "sale_attachments"
            });
        }

        public async Task<SaleAttachment> UploadAsync(IFormFile file, string category, CancellationToken cancellationToken = default)
        {
            ValidateFile(file, category);

            var normalizedCategory = category.Trim();
            var uploadedAt = DateTime.UtcNow;
            var fileName = Path.GetFileName(file.FileName);

            var options = new GridFSUploadOptions
            {
                Metadata = new BsonDocument
                {
                    { "contentType", file.ContentType ?? "application/octet-stream" },
                    { "originalFileName", fileName },
                    { "category", normalizedCategory },
                    { "uploadedAt", uploadedAt }
                }
            };

            await using var stream = file.OpenReadStream();
            var fileId = await _bucket.UploadFromStreamAsync(fileName, stream, options, cancellationToken);

            return new SaleAttachment
            {
                StorageId = fileId.ToString(),
                FileName = fileName,
                ContentType = file.ContentType ?? "application/octet-stream",
                SizeBytes = file.Length,
                Category = normalizedCategory,
                UploadedAt = uploadedAt
            };
        }

        public async Task<StoredSaleAttachment?> OpenReadAsync(string storageId, CancellationToken cancellationToken = default)
        {
            if (!ObjectId.TryParse(storageId, out var fileId))
            {
                return null;
            }

            var fileInfo = await _bucket
                .Find(Builders<GridFSFileInfo>.Filter.Eq(info => info.Id, fileId))
                .FirstOrDefaultAsync(cancellationToken);

            if (fileInfo is null)
            {
                return null;
            }

            var contentType = fileInfo.Metadata?.GetValue("contentType", "application/octet-stream").AsString
                ?? "application/octet-stream";
            var stream = await _bucket.OpenDownloadStreamAsync(fileId, cancellationToken: cancellationToken);

            return new StoredSaleAttachment(stream, fileInfo.Filename, contentType, fileInfo.Length);
        }

        public async Task DeleteAsync(string storageId, CancellationToken cancellationToken = default)
        {
            if (!ObjectId.TryParse(storageId, out var fileId))
            {
                return;
            }

            try
            {
                await _bucket.DeleteAsync(fileId, cancellationToken);
            }
            catch (GridFSFileNotFoundException)
            {
            }
        }

        public async Task DeleteManyAsync(IEnumerable<string> storageIds, CancellationToken cancellationToken = default)
        {
            foreach (var storageId in storageIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.Ordinal))
            {
                await DeleteAsync(storageId, cancellationToken);
            }
        }

        private static void ValidateFile(IFormFile file, string category)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("Nenhum arquivo foi enviado.");
            }

            if (!SaleAttachmentCategories.IsValid(category))
            {
                throw new ArgumentException("Categoria de anexo inválida.");
            }

            var extension = Path.GetExtension(file.FileName);

            if (SaleAttachmentCategories.IsImage(category))
            {
                if (string.IsNullOrWhiteSpace(file.ContentType) || !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException("A categoria selecionada aceita apenas imagens.");
                }

                if (!AllowedImageExtensions.Contains(extension))
                {
                    throw new ArgumentException("Formato de imagem não suportado.");
                }

                if (file.Length > MaxImageBytes)
                {
                    throw new ArgumentException("Imagens devem ter no máximo 2 MB.");
                }

                return;
            }

            if (!AllowedFileExtensions.Contains(extension))
            {
                throw new ArgumentException("Formato de arquivo complementar não suportado.");
            }

            if (file.Length > MaxFileBytes)
            {
                throw new ArgumentException("Arquivos complementares devem ter no máximo 8 MB.");
            }
        }
    }
}