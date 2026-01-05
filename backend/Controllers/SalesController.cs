using Byte2Life.API.Models;
using Byte2Life.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Byte2Life.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SalesController : ControllerBase
    {
        private readonly ISaleService _saleService;

        public SalesController(ISaleService saleService) =>
            _saleService = saleService;

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

        [HttpDelete("{id:length(24)}")]
        public async Task<IActionResult> Delete(string id)
        {
            var sale = await _saleService.GetByIdAsync(id);

            if (sale is null)
            {
                return NotFound();
            }

            await _saleService.RemoveAsync(id);

            return NoContent();
        }
    }
}
