using Byte2Life.API.Models;
using Byte2Life.API.Services;
using LiteDB;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace Byte2Life.API.Controllers
{
    [ApiController]
    [Route("api/stock")]
    public class StockController : ControllerBase
    {
        private readonly LiteDatabase _db;
        private readonly ILiteCollection<StockItem> _collection;
        private readonly IWebHostEnvironment _env;
        private readonly IBudgetService _budgetService;

        public StockController(LiteDatabase db, IWebHostEnvironment env, IBudgetService budgetService)
        {
            _db = db;
            _collection = _db.GetCollection<StockItem>("stock");
            _env = env;
            _budgetService = budgetService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<StockItem>>> GetAll()
        {
            var items = _collection.FindAll().OrderByDescending(x => x.CreatedAt).ToList();
            bool updated = false;

            foreach (var item in items)
            {
                if (item.ProductionCost == 0 && !string.IsNullOrEmpty(item.FilamentId) && item.WeightGrams > 0)
                {
                    await CalculateCost(item);
                    _collection.Update(item);
                    updated = true;
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
            var item = _collection.FindById(new ObjectId(id));
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<StockItem>> Create(StockItem item)
        {
            if (item.Id == null) item.Id = ObjectId.NewObjectId();
            
            // Only calculate if not provided by frontend
            if (item.ProductionCost == 0)
            {
                await CalculateCost(item);
            }

            _collection.Insert(item);
            return CreatedAtAction(nameof(GetById), new { id = item.Id.ToString() }, item);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, StockItem item)
        {
            var existing = _collection.FindById(new ObjectId(id));
            if (existing == null) return NotFound();

            item.Id = new ObjectId(id);
            
            // Only calculate if not provided by frontend (trust the user/frontend calculation)
            if (item.ProductionCost == 0)
            {
                await CalculateCost(item);
            }

            _collection.Update(item);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(string id)
        {
            var item = _collection.FindById(new ObjectId(id));
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

            _collection.Delete(new ObjectId(id));
            return NoContent();
        }

        [HttpPost("from-sale/{saleId}")]
        public IActionResult MoveFromSale(string saleId)
        {
            var salesCollection = _db.GetCollection<Sale>("sales");
            var sale = salesCollection.FindById(new ObjectId(saleId));
            
            if (sale == null) return NotFound("Venda não encontrada");

            // Check if this sale is linked to an existing stock item
            if (sale.StockItemId != null)
            {
                var existingStockItem = _collection.FindById(sale.StockItemId);
                if (existingStockItem != null)
                {
                    // Restore the existing item
                    existingStockItem.Status = "Available";
                    _collection.Update(existingStockItem);
                    salesCollection.Delete(new ObjectId(saleId));
                    return Ok(existingStockItem);
                }
            }

            // Fallback: Create new item if no link or link broken
            var stockItem = new StockItem
            {
                Id = ObjectId.NewObjectId(),
                Description = sale.Description,
                FilamentId = sale.FilamentId?.ToString() ?? "",
                PrintTime = sale.DesignPrintTime ?? "",
                WeightGrams = sale.MassGrams,
                Cost = (double)sale.Cost,
                Status = "Available",
                CreatedAt = DateTime.Now
            };

            _collection.Insert(stockItem);
            salesCollection.Delete(new ObjectId(saleId));

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
