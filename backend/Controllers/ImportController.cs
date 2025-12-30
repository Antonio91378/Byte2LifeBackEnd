using Byte2Life.API.Models;
using Byte2Life.API.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace Byte2Life.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImportController : ControllerBase
    {
        private readonly IFilamentService _filamentService;
        private readonly IClientService _clientService;
        private readonly ISaleService _saleService;

        public ImportController(IFilamentService filamentService, IClientService clientService, ISaleService saleService)
        {
            _filamentService = filamentService;
            _clientService = clientService;
            _saleService = saleService;
        }

        [HttpPost]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is empty");

            using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);

            // Skip header
            await reader.ReadLineAsync();

            var result = new ImportResult();
            int lineNumber = 1; // Header is line 1

            while (!reader.EndOfStream)
            {
                lineNumber++;
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    var columns = line.Split(';');
                    if (columns.Length < 22)
                    {
                        result.Errors.Add($"Line {lineNumber}: Insufficient columns. Expected 22, got {columns.Length}.");
                        result.FailureCount++;
                        continue;
                    }

                    var description = columns[0].Trim();
                    var linkFilament = columns[1].Trim();
                    var priceFilamentStr = columns[2].Trim();
                    var filamentDesc = columns[3].Trim();
                    var filamentColor = columns[4].Trim();
                    var linkProduct = columns[5].Trim();
                    var printQuality = columns[6].Trim();
                    var massStr = columns[7].Trim();
                    var costStr = columns[8].Trim();
                    var saleValueStr = columns[9].Trim();
                    var profitStr = columns[10].Trim();
                    var profitPercent = columns[11].Trim();
                    var clientSex = columns[12].Trim();
                    var category = columns[13].Trim();
                    var clientNumber = columns[14].Trim();
                    var clientName = columns[15].Trim();
                    var printConcluded = columns[16].Trim();
                    var productDelivered = columns[17].Trim();
                    var productPaid = columns[18].Trim();
                    var timeDesignPrint = columns[19].Trim();
                    var printStatus = columns[20].Trim();
                    var saleDateStr = columns[21].Trim();
                    var deliveryDateStr = columns.Length > 22 ? columns[22].Trim() : null;

                    var culture = System.Globalization.CultureInfo.InvariantCulture;

                    decimal priceFilament, cost, saleValue, profit;
                    double mass;

                    try { priceFilament = ParseDecimal(priceFilamentStr); } catch { throw new Exception($"Invalid PriceFilament: {priceFilamentStr}"); }
                    try { mass = ParseDouble(massStr); } catch { throw new Exception($"Invalid Mass: {massStr}"); }
                    try { cost = ParseDecimal(costStr); } catch { throw new Exception($"Invalid Cost: {costStr}"); }
                    try { saleValue = ParseDecimal(saleValueStr); } catch { throw new Exception($"Invalid SaleValue: {saleValueStr}"); }
                    try { profit = ParseDecimal(profitStr); } catch { throw new Exception($"Invalid Profit: {profitStr}"); }
                    
                    DateTime saleDate = DateTime.Now;
                    if (!string.IsNullOrWhiteSpace(saleDateStr))
                    {
                        if (!DateTime.TryParse(saleDateStr, culture, System.Globalization.DateTimeStyles.None, out saleDate))
                        {
                             // Try parsing with pt-BR culture if invariant fails, common in Brazil
                             if (!DateTime.TryParse(saleDateStr, new System.Globalization.CultureInfo("pt-BR"), System.Globalization.DateTimeStyles.None, out saleDate))
                             {
                                 throw new Exception($"Invalid SaleDate: {saleDateStr}");
                             }
                        }
                    }

                    DateTime? deliveryDate = null;
                    if (!string.IsNullOrWhiteSpace(deliveryDateStr))
                    {
                        if (DateTime.TryParse(deliveryDateStr, culture, System.Globalization.DateTimeStyles.None, out var dDate))
                        {
                            deliveryDate = dDate;
                        }
                        else if (DateTime.TryParse(deliveryDateStr, new System.Globalization.CultureInfo("pt-BR"), System.Globalization.DateTimeStyles.None, out var dDateBr))
                        {
                            deliveryDate = dDateBr;
                        }
                    }

                    var filament = (await _filamentService.GetAsync())
                        .FirstOrDefault(f => f.Description == filamentDesc && f.Link == linkFilament);

                    if (filament == null)
                    {
                        filament = new Filament
                        {
                            Description = filamentDesc,
                            Link = linkFilament,
                            Price = priceFilament,
                            InitialMassGrams = 1000,
                            RemainingMassGrams = 1000,
                            Color = string.IsNullOrWhiteSpace(filamentColor) ? "Desconhecida" : filamentColor
                        };
                        await _filamentService.CreateAsync(filament);
                    }

                    var client = (await _clientService.GetAsync())
                        .FirstOrDefault(c => c.PhoneNumber == clientNumber);

                    if (client == null)
                    {
                        client = new Client
                        {
                            PhoneNumber = clientNumber,
                            Sex = clientSex,
                            Category = category,
                            Name = string.IsNullOrWhiteSpace(clientName) ? "Cliente Importado" : clientName
                        };
                        await _clientService.CreateAsync(client);
                    }
                    else
                    {
                        // Update existing client
                        bool changed = false;
                        if (client.Sex != clientSex) { client.Sex = clientSex; changed = true; }
                        if (client.Category != category) { client.Category = category; changed = true; }
                        if (!string.IsNullOrWhiteSpace(clientName) && client.Name != clientName) { client.Name = clientName; changed = true; }
                        
                        if (changed)
                        {
                            await _clientService.UpdateAsync(client.Id!.ToString(), client);
                        }
                    }

                    var sale = new Sale
                    {
                        Description = description,
                        ProductLink = linkProduct,
                        PrintQuality = printQuality,
                        MassGrams = mass,
                        Cost = cost,
                        SaleValue = saleValue,
                        Profit = profit,
                        ProfitPercentage = profitPercent,
                        DesignPrintTime = timeDesignPrint,
                        PrintStatus = string.IsNullOrWhiteSpace(printStatus) ? "Pending" : printStatus,
                        IsPrintConcluded = printConcluded?.ToUpper() == "S",
                        IsDelivered = productDelivered?.ToUpper() == "S",
                        IsPaid = productPaid?.ToUpper() == "S",
                        FilamentId = filament.Id,
                        ClientId = client.Id,
                        SaleDate = saleDate,
                        DeliveryDate = deliveryDate
                    };

                    await _saleService.CreateAsync(sale);
                    result.SuccessCount++;
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Line {lineNumber}: {ex.Message}");
                    result.FailureCount++;
                }
            }

            return Ok(result);
        }

        private decimal ParseDecimal(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return 0;
            input = input.Trim();
            int lastComma = input.LastIndexOf(',');
            int lastDot = input.LastIndexOf('.');
            if (lastComma > lastDot)
            {
                if (decimal.TryParse(input, System.Globalization.NumberStyles.Any, new System.Globalization.CultureInfo("pt-BR"), out var res)) return res;
            }
            if (decimal.TryParse(input, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var resInv)) return resInv;
            throw new Exception($"Invalid number format: {input}");
        }

        private double ParseDouble(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return 0;
            input = input.Trim();
            int lastComma = input.LastIndexOf(',');
            int lastDot = input.LastIndexOf('.');
            if (lastComma > lastDot)
            {
                if (double.TryParse(input, System.Globalization.NumberStyles.Any, new System.Globalization.CultureInfo("pt-BR"), out var res)) return res;
            }
            if (double.TryParse(input, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var resInv)) return resInv;
            throw new Exception($"Invalid number format: {input}");
        }

        public class ImportResult
        {
            public int SuccessCount { get; set; }
            public int FailureCount { get; set; }
            public List<string> Errors { get; set; } = new();
        }
    }
}
