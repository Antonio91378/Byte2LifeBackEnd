using Byte2Life.API.Models;
using Byte2Life.API.Services;
using Microsoft.AspNetCore.Mvc;

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
        public async Task<IActionResult> Post(Filament newFilament)
        {
            await _filamentService.CreateAsync(newFilament);

            return CreatedAtAction(nameof(Get), new { id = newFilament.Id }, newFilament);
        }

        [HttpPut("{id:length(24)}")]
        public async Task<IActionResult> Update(string id, Filament updatedFilament)
        {
            var filament = await _filamentService.GetAsync(id);

            if (filament is null)
            {
                return NotFound();
            }

            updatedFilament.Id = filament.Id;

            await _filamentService.UpdateAsync(id, updatedFilament);

            return NoContent();
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
