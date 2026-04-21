using Byte2Life.API.Models;
using Byte2Life.API.Persistence;
using Byte2Life.API.Services;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Globalization;

namespace Byte2Life.API.Controllers
{
    [ApiController]
    [Route("api/stock")]
    public class StockController : ControllerBase
    {
        private readonly IMongoCollection<StockItem> _collection;
        private readonly IMongoCollection<Sale> _salesCollection;
        private readonly IWebHostEnvironment _env;
        private readonly IBudgetService _budgetService;
        private readonly ISaleAttachmentStorageService _saleAttachmentStorageService;

        public StockController(
            IMongoDatabase database,
            IWebHostEnvironment env,
            IBudgetService budgetService,
            ISaleAttachmentStorageService saleAttachmentStorageService)
        {
            _collection = database.GetCollection<StockItem>(MongoCollectionNames.Stock);
            _salesCollection = database.GetCollection<Sale>(MongoCollectionNames.Sales);
            _env = env;
            _budgetService = budgetService;
            _saleAttachmentStorageService = saleAttachmentStorageService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<StockItem>>> GetAll()
        {
            var items = _collection.Find(FilterDefinition<StockItem>.Empty)
                .ToList()
                .OrderByDescending(x => x.CreatedAt)
                .ToList();

            foreach (var item in items)
            {
                if (item.ProductionCost == 0 && !string.IsNullOrEmpty(item.FilamentId) && item.WeightGrams > 0)
                {
                    await CalculateCost(item);
                    _collection.ReplaceOne(MongoId.FilterById<StockItem>(item.Id!.Value), item);
                }
            }

            return Ok(items);
        }

        private async Task CalculateCost(StockItem item)
        {
            if (!string.IsNullOrEmpty(item.FilamentId) && item.WeightGrams > 0)
            {
                try
                {
                    var timeHours = ParsePrintTime(item.PrintTime);
                    var quality = ParseQuality(item.PrintQuality);

                    var budget = await _budgetService.CalculateBudgetAsync(new BudgetRequest
                    {
                        FilamentId = item.FilamentId,
                        DetailLevel = quality,
                        MassGrams = item.WeightGrams,
                        HasCustomArt = item.HasCustomArt,
                        HasPainting = item.HasPainting,
                        HasVarnish = item.HasVarnish,
                        PrintTimeHours = timeHours > 0 ? timeHours : null
                    });

                    item.ProductionCost = (double)budget.TotalProductionCost;
                    item.NozzleDiameter = budget.NozzleDiameter;
                    item.LayerHeight = budget.LayerHeightRange;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error calculating cost: {ex.Message}");
                }
            }
        }

        private DetailLevel ParseQuality(string quality)
        {
            return quality switch
            {
                "Baixo" => DetailLevel.Low,
                "Normal" => DetailLevel.Normal,
                "Alto" => DetailLevel.High,
                "Extremo" => DetailLevel.Extreme,
                "Draft" => DetailLevel.Low,
                "Standard" => DetailLevel.Normal,
                "High" => DetailLevel.High,
                "Ultra" => DetailLevel.Extreme,
                _ => DetailLevel.Normal
            };
        }

        private double ParsePrintTime(string time)
        {
            if (string.IsNullOrEmpty(time)) return 0;
            double total = 0;
            var parts = time.ToLower().Split(' ');
            foreach (var part in parts)
            {
                // Normalize separators to ensure dot is used
                var cleanPart = part.Replace(',', '.');

                if (cleanPart.EndsWith("h"))
                {
                    if (double.TryParse(cleanPart.TrimEnd('h'), NumberStyles.Any, CultureInfo.InvariantCulture, out double h)) total += h;
                }
                else if (cleanPart.EndsWith("m"))
                {
                    if (double.TryParse(cleanPart.TrimEnd('m'), NumberStyles.Any, CultureInfo.InvariantCulture, out double m)) total += m / 60.0;
                }
                else if (double.TryParse(cleanPart, NumberStyles.Any, CultureInfo.InvariantCulture, out double val)) // Handle plain numbers as hours
                {
                    total += val;
                }
            }
            return total;
        }

        [HttpGet("{id}")]
        public ActionResult<StockItem> GetById(string id)
        {
            var item = _collection.Find(MongoId.FilterById<StockItem>(id)).FirstOrDefault();
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<StockItem>> Create(StockItem item)
        {
            if (!item.Id.HasValue || item.Id.Value == ObjectId.Empty) item.Id = MongoId.New();
            
            // Only calculate if not provided by frontend
            if (item.ProductionCost == 0)
            {
                await CalculateCost(item);
            }

            _collection.InsertOne(item);
            return CreatedAtAction(nameof(GetById), new { id = item.Id.ToString() }, item);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, StockItem item)
        {
            var existing = _collection.Find(MongoId.FilterById<StockItem>(id)).FirstOrDefault();
            if (existing == null) return NotFound();

            item.Id = existing.Id;
            
            // Only calculate if not provided by frontend (trust the user/frontend calculation)
            if (item.ProductionCost == 0)
            {
                await CalculateCost(item);
            }

            _collection.ReplaceOne(MongoId.FilterById<StockItem>(id), item);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(string id)
        {
            var item = _collection.Find(MongoId.FilterById<StockItem>(id)).FirstOrDefault();
            if (item == null) return NotFound();

            // Delete associated photos
            foreach (var photo in item.Photos)
            {
                string webRootPath = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
                var path = Path.Combine(webRootPath, photo.TrimStart('/').Replace("/", "\\"));
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }
            }

            _collection.DeleteOne(MongoId.FilterById<StockItem>(id));
            return NoContent();
        }

        [HttpPost("from-sale/{saleId}")]
        public async Task<IActionResult> MoveFromSale(string saleId)
        {
            var sale = _salesCollection.Find(MongoId.FilterById<Sale>(saleId)).FirstOrDefault();
            
            if (sale == null) return NotFound("Venda não encontrada");

            // Check if this sale is linked to an existing stock item
            if (sale.StockItemId.HasValue)
            {
                var existingStockItem = _collection.Find(MongoId.FilterById<StockItem>(sale.StockItemId.Value)).FirstOrDefault();
                if (existingStockItem != null)
                {
                    // Restore the existing item
                    existingStockItem.Status = "Available";
                    _collection.ReplaceOne(MongoId.FilterById<StockItem>(existingStockItem.Id!.Value), existingStockItem);
                    await _saleAttachmentStorageService.DeleteManyAsync(sale.Attachments.Select(attachment => attachment.StorageId));
                    _salesCollection.DeleteOne(MongoId.FilterById<Sale>(saleId));
                    return Ok(existingStockItem);
                }
            }

            // Fallback: Create new item if no link or link broken
            var stockItem = new StockItem
            {
                Id = MongoId.New(),
                Description = sale.Description,
                FilamentId = sale.FilamentId?.ToString() ?? "",
                PrintTime = sale.DesignPrintTime ?? "",
                WeightGrams = sale.MassGrams,
                Cost = (double)sale.Cost,
                Status = "Available",
                CreatedAt = DateTime.Now
            };

            _collection.InsertOne(stockItem);
            await _saleAttachmentStorageService.DeleteManyAsync(sale.Attachments.Select(attachment => attachment.StorageId));
            _salesCollection.DeleteOne(MongoId.FilterById<Sale>(saleId));

            return Ok(stockItem);
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadPhoto(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            string webRootPath = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var uploadsFolder = Path.Combine(webRootPath, "uploads");
            
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Ok(new { url = $"/uploads/{fileName}" });
        }
    }
}
