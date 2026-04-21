using Byte2Life.API.Models;
using Byte2Life.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Byte2Life.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SalesController : ControllerBase
    {
        private readonly ISaleService _saleService;
        private readonly ISaleAttachmentStorageService _saleAttachmentStorageService;
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        private const string SalePayloadFieldName = "sale";
        private const string ProductImagesFieldName = "productImages";
        private const string ComplementaryImagesFieldName = "complementaryImages";
        private const string ComplementaryFilesFieldName = "complementaryFiles";

        public SalesController(
            ISaleService saleService,
            ISaleAttachmentStorageService saleAttachmentStorageService,
            IOptions<JsonOptions> jsonOptions)
        {
            _saleService = saleService;
            _saleAttachmentStorageService = saleAttachmentStorageService;
            _jsonSerializerOptions = jsonOptions.Value.JsonSerializerOptions;
        }

        [HttpGet]
        public async Task<List<Sale>> Get([FromQuery] string? date = null) =>
            await _saleService.GetAsync(date);

        [HttpGet("{id:length(24)}")]
        public async Task<ActionResult<Sale>> GetById(string id)
        {
            var sale = await _saleService.GetByIdAsync(id);

            if (sale is null)
            {
                return NotFound();
            }

            return sale;
        }

        [HttpGet("queue")]
        public async Task<List<Sale>> GetQueue() =>
            await _saleService.GetQueueAsync();

        [HttpGet("painting")]
        public async Task<List<Sale>> GetPaintingSchedule() =>
            await _saleService.GetPaintingScheduleAsync();

        [HttpGet("services")]
        public async Task<List<Sale>> GetServiceSchedule() =>
            await _saleService.GetServiceScheduleAsync();

        [HttpGet("current")]
        public async Task<ActionResult<Sale>> GetCurrentPrint()
        {
            var sale = await _saleService.GetCurrentPrintAsync();
            if (sale is null)
            {
                return NoContent();
            }
            return sale;
        }

        [HttpPost]
        public async Task<IActionResult> Post(Sale newSale)
        {
            try 
            {
                await _saleService.CreateAsync(newSale);
                return CreatedAtAction(nameof(GetById), new { id = newSale.Id }, newSale);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("with-media")]
        public async Task<IActionResult> PostWithMedia(CancellationToken cancellationToken)
        {
            var uploadedAttachments = new List<SaleAttachment>();

            try
            {
                var (newSale, form) = await ParseMultipartSaleRequestAsync(cancellationToken);
                uploadedAttachments = await UploadAttachmentsFromFormAsync(form, cancellationToken);
                newSale.Attachments = uploadedAttachments;

                await _saleService.CreateAsync(newSale);
                return CreatedAtAction(nameof(GetById), new { id = newSale.Id }, newSale);
            }
            catch (ArgumentException ex)
            {
                await DeleteUploadedAttachmentsAsync(uploadedAttachments, cancellationToken);
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                await DeleteUploadedAttachmentsAsync(uploadedAttachments, cancellationToken);
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id:length(24)}")]
        public async Task<IActionResult> Update(string id, Sale updatedSale)
        {
            var sale = await _saleService.GetByIdAsync(id);

            if (sale is null)
            {
                return NotFound();
            }

            updatedSale.Id = sale.Id;

            try
            {
                await _saleService.UpdateAsync(id, updatedSale);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id:length(24)}/with-media")]
        public async Task<IActionResult> UpdateWithMedia(string id, CancellationToken cancellationToken)
        {
            var existingSale = await _saleService.GetByIdAsync(id);

            if (existingSale is null)
            {
                return NotFound();
            }

            var uploadedAttachments = new List<SaleAttachment>();

            try
            {
                var (updatedSale, form) = await ParseMultipartSaleRequestAsync(cancellationToken);
                updatedSale.Id = existingSale.Id;

                var retainedAttachments = KeepExistingAttachments(existingSale.Attachments, updatedSale.Attachments);
                uploadedAttachments = await UploadAttachmentsFromFormAsync(form, cancellationToken);
                updatedSale.Attachments = retainedAttachments.Concat(uploadedAttachments).ToList();

                await _saleService.UpdateAsync(id, updatedSale);

                var removedStorageIds = existingSale.Attachments
                    .Select(attachment => attachment.StorageId)
                    .Except(updatedSale.Attachments.Select(attachment => attachment.StorageId), StringComparer.Ordinal)
                    .ToList();

                await _saleAttachmentStorageService.DeleteManyAsync(removedStorageIds, cancellationToken);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                await DeleteUploadedAttachmentsAsync(uploadedAttachments, cancellationToken);
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                await DeleteUploadedAttachmentsAsync(uploadedAttachments, cancellationToken);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("files/{fileId:length(24)}")]
        public async Task<IActionResult> GetAttachmentFile(string fileId, CancellationToken cancellationToken)
        {
            var storedAttachment = await _saleAttachmentStorageService.OpenReadAsync(fileId, cancellationToken);

            if (storedAttachment is null)
            {
                return NotFound();
            }

            return File(storedAttachment.Content, storedAttachment.ContentType, enableRangeProcessing: true);
        }

        [HttpPatch("{id:length(24)}/schedule")]
        public async Task<IActionResult> UpdateSchedule(string id, [FromBody] SaleScheduleUpdate update)
        {
            var sale = await _saleService.GetByIdAsync(id);

            if (sale is null)
            {
                return NotFound();
            }

            if (update is null)
            {
                return BadRequest("Invalid payload");
            }

            try
            {
                await _saleService.UpdateScheduleAsync(id, update.PrintStartConfirmedAt);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }

            return NoContent();
        }

        [HttpPatch("{id:length(24)}/paint-schedule")]
        public async Task<IActionResult> UpdatePaintSchedule(string id, [FromBody] SalePaintScheduleUpdate update)
        {
            var sale = await _saleService.GetByIdAsync(id);

            if (sale is null)
            {
                return NotFound();
            }

            if (update is null)
            {
                return BadRequest("Invalid payload");
            }

            try
            {
                await _saleService.UpdatePaintScheduleAsync(id, update.PaintStartConfirmedAt, update.PaintTimeHours, update.PaintResponsible);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }

            return NoContent();
        }

        [HttpPatch("{id:length(24)}/design-schedule")]
        public async Task<IActionResult> UpdateDesignSchedule(string id, [FromBody] SaleDesignScheduleUpdate update)
        {
            var sale = await _saleService.GetByIdAsync(id);

            if (sale is null)
            {
                return NotFound();
            }

            if (update is null)
            {
                return BadRequest("Invalid payload");
            }

            try
            {
                await _saleService.UpdateDesignScheduleAsync(id, update.DesignStartConfirmedAt, update.DesignTimeHours, update.DesignResponsible, update.DesignValue);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }

            return NoContent();
        }

        [HttpPatch("{id:length(24)}/design-status")]
        public async Task<IActionResult> UpdateDesignStatus(string id, [FromBody] SaleDesignStatusUpdate update)
        {
            var sale = await _saleService.GetByIdAsync(id);

            if (sale is null)
            {
                return NotFound();
            }

            if (update is null)
            {
                return BadRequest("Invalid payload");
            }

            await _saleService.UpdateDesignStatusAsync(id, update.DesignStatus);

            return NoContent();
        }

        [HttpPatch("{id:length(24)}/paint-status")]
        public async Task<IActionResult> UpdatePaintStatus(string id, [FromBody] SalePaintStatusUpdate update)
        {
            var sale = await _saleService.GetByIdAsync(id);

            if (sale is null)
            {
                return NotFound();
            }

            if (update is null)
            {
                return BadRequest("Invalid payload");
            }

            await _saleService.UpdatePaintStatusAsync(id, update.PaintStatus);

            return NoContent();
        }

        [HttpDelete("{id:length(24)}")]
        public async Task<IActionResult> Delete(string id)
        {
            var sale = await _saleService.GetByIdAsync(id);

            if (sale is null)
            {
                return NotFound();
            }

            await _saleService.RemoveAsync(id);
            await _saleAttachmentStorageService.DeleteManyAsync(sale.Attachments.Select(attachment => attachment.StorageId));

            return NoContent();
        }

        private async Task<(Sale Sale, IFormCollection Form)> ParseMultipartSaleRequestAsync(CancellationToken cancellationToken)
        {
            if (!Request.HasFormContentType)
            {
                throw new ArgumentException("O envio de mídia deve usar multipart/form-data.");
            }

            var form = await Request.ReadFormAsync(cancellationToken);
            var saleJson = form[SalePayloadFieldName].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(saleJson))
            {
                throw new ArgumentException("Os dados da venda não foram enviados.");
            }

            var sale = JsonSerializer.Deserialize<Sale>(saleJson, _jsonSerializerOptions);
            if (sale is null)
            {
                throw new ArgumentException("Não foi possível interpretar os dados da venda.");
            }

            sale.Attachments ??= new List<SaleAttachment>();
            return (sale, form);
        }

        private async Task<List<SaleAttachment>> UploadAttachmentsFromFormAsync(IFormCollection form, CancellationToken cancellationToken)
        {
            var attachments = new List<SaleAttachment>();

            attachments.AddRange(await UploadCategoryFilesAsync(
                GetFormFiles(form, ProductImagesFieldName),
                SaleAttachmentCategories.ProductImage,
                cancellationToken));

            attachments.AddRange(await UploadCategoryFilesAsync(
                GetFormFiles(form, ComplementaryImagesFieldName),
                SaleAttachmentCategories.ComplementaryImage,
                cancellationToken));

            attachments.AddRange(await UploadCategoryFilesAsync(
                GetFormFiles(form, ComplementaryFilesFieldName),
                SaleAttachmentCategories.ComplementaryFile,
                cancellationToken));

            return attachments;
        }

        private async Task<List<SaleAttachment>> UploadCategoryFilesAsync(
            IEnumerable<IFormFile> files,
            string category,
            CancellationToken cancellationToken)
        {
            var attachments = new List<SaleAttachment>();

            foreach (var file in files)
            {
                attachments.Add(await _saleAttachmentStorageService.UploadAsync(file, category, cancellationToken));
            }

            return attachments;
        }

        private static List<IFormFile> GetFormFiles(IFormCollection form, string fieldName)
        {
            return form.Files
                .Where(file => string.Equals(file.Name, fieldName, StringComparison.Ordinal))
                .ToList();
        }

        private static List<SaleAttachment> KeepExistingAttachments(
            IEnumerable<SaleAttachment> existingAttachments,
            IEnumerable<SaleAttachment> requestedAttachments)
        {
            var requestedIds = new HashSet<string>(
                requestedAttachments
                    .Where(attachment => !string.IsNullOrWhiteSpace(attachment.StorageId))
                    .Select(attachment => attachment.StorageId),
                StringComparer.Ordinal);

            return existingAttachments
                .Where(attachment => requestedIds.Contains(attachment.StorageId))
                .ToList();
        }

        private Task DeleteUploadedAttachmentsAsync(IEnumerable<SaleAttachment> uploadedAttachments, CancellationToken cancellationToken)
        {
            return _saleAttachmentStorageService.DeleteManyAsync(
                uploadedAttachments.Select(attachment => attachment.StorageId),
                cancellationToken);
        }
    }
}
