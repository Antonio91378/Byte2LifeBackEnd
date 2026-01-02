using Byte2Life.API.Models;
using Byte2Life.API.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Byte2Life.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FilamentsController : ControllerBase
    {
        private readonly IFilamentService _filamentService;
        private readonly ISaleService _saleService;

        public FilamentsController(IFilamentService filamentService, ISaleService saleService)
        {
            _filamentService = filamentService;
            _saleService = saleService;
        }

        [HttpGet]
        public async Task<List<Filament>> Get() =>
            await _filamentService.GetAsync();

        [HttpGet("{id:length(24)}")]
        public async Task<ActionResult<Filament>> Get(string id)
        {
            var filament = await _filamentService.GetAsync(id);

            if (filament is null)
            {
                return NotFound();
            }

            return filament;
        }

        [HttpGet("{id:length(24)}/sales")]
        public async Task<ActionResult<List<Sale>>> GetSales(string id)
        {
            var sales = await _saleService.GetByFilamentIdAsync(id);
            return sales;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] JsonElement payload)
        {
            var newFilament = new Filament
            {
                Description = GetString(payload, "description"),
                Link = GetString(payload, "link"),
                Price = GetDecimal(payload, "price"),
                InitialMassGrams = GetDouble(payload, "initialMassGrams"),
                RemainingMassGrams = GetDouble(payload, "remainingMassGrams"),
                Color = GetString(payload, "color"),
                ColorHex = GetString(payload, "colorHex"),
                Type = GetString(payload, "type"),
                WarningComment = GetString(payload, "warningComment"),
                SlicingProfile3mfPath = GetString(payload, "slicingProfile3mfPath")
            };

            await _filamentService.CreateAsync(newFilament);

            return CreatedAtAction(nameof(Get), new { id = newFilament.Id }, newFilament);
        }

        [HttpPut("{id:length(24)}")]
        public async Task<IActionResult> Update(string id, [FromBody] JsonElement payload)
        {
            var filament = await _filamentService.GetAsync(id);

            if (filament is null)
            {
                return NotFound();
            }

            filament.Description = GetString(payload, "description", filament.Description);
            filament.Link = GetString(payload, "link", filament.Link);
            filament.Price = GetDecimal(payload, "price", filament.Price);
            filament.InitialMassGrams = GetDouble(payload, "initialMassGrams", filament.InitialMassGrams);
            filament.RemainingMassGrams = GetDouble(payload, "remainingMassGrams", filament.RemainingMassGrams);
            filament.Color = GetString(payload, "color", filament.Color);
            filament.ColorHex = GetString(payload, "colorHex", filament.ColorHex);
            filament.Type = GetString(payload, "type", filament.Type);
            filament.WarningComment = GetString(payload, "warningComment", filament.WarningComment);
            filament.SlicingProfile3mfPath = GetString(payload, "slicingProfile3mfPath", filament.SlicingProfile3mfPath);

            await _filamentService.UpdateAsync(id, filament);

            return NoContent();
        }

        private static string GetString(JsonElement payload, string propertyName, string? fallback = null)
        {
            if (!payload.TryGetProperty(propertyName, out var value))
            {
                return fallback ?? "";
            }

            if (value.ValueKind == JsonValueKind.String)
            {
                return value.GetString() ?? "";
            }

            if (value.ValueKind == JsonValueKind.Null)
            {
                return fallback ?? "";
            }

            return fallback ?? "";
        }

        private static double GetDouble(JsonElement payload, string propertyName, double fallback = 0)
        {
            if (!payload.TryGetProperty(propertyName, out var value))
            {
                return fallback;
            }

            return value.TryGetDouble(out var parsed) ? parsed : fallback;
        }

        private static decimal GetDecimal(JsonElement payload, string propertyName, decimal fallback = 0)
        {
            if (!payload.TryGetProperty(propertyName, out var value))
            {
                return fallback;
            }

            return value.TryGetDecimal(out var parsed) ? parsed : fallback;
        }

        [HttpDelete("{id:length(24)}")]
        public async Task<IActionResult> Delete(string id)
        {
            var filament = await _filamentService.GetAsync(id);

            if (filament is null)
            {
                return NotFound();
            }

            try
            {
                await _filamentService.RemoveAsync(id);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }

            return NoContent();
        }
    }
}
