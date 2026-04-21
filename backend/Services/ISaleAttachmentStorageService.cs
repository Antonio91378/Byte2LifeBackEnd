using Byte2Life.API.Models;
using Microsoft.AspNetCore.Http;

namespace Byte2Life.API.Services
{
    public record StoredSaleAttachment(
        Stream Content,
        string FileName,
        string ContentType,
        long SizeBytes);

    public interface ISaleAttachmentStorageService
    {
        Task<SaleAttachment> UploadAsync(IFormFile file, string category, CancellationToken cancellationToken = default);
        Task<StoredSaleAttachment?> OpenReadAsync(string storageId, CancellationToken cancellationToken = default);
        Task DeleteAsync(string storageId, CancellationToken cancellationToken = default);
        Task DeleteManyAsync(IEnumerable<string> storageIds, CancellationToken cancellationToken = default);
    }
}