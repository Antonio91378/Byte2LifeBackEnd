using Byte2Life.API.Models;
using Byte2Life.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Byte2Life.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaintsController : ControllerBase
    {
        private readonly IPaintService _paintService;

        public PaintsController(IPaintService paintService)
        {
            _paintService = paintService;
        }

        [HttpGet]
        public async Task<List<Paint>> Get() =>
            await _paintService.GetAsync();

        [HttpGet("{id:length(24)}")]
        public async Task<ActionResult<Paint>> Get(string id)
        {
            var paint = await _paintService.GetAsync(id);

            if (paint is null)
            {
                return NotFound();
            }

            return paint;
        }

        [HttpPost]
        public async Task<IActionResult> Post(Paint newPaint)
        {
            await _paintService.CreateAsync(newPaint);

            return CreatedAtAction(nameof(Get), new { id = newPaint.Id }, newPaint);
        }

        [HttpPut("{id:length(24)}")]
        public async Task<IActionResult> Update(string id, Paint updatedPaint)
        {
            var paint = await _paintService.GetAsync(id);

            if (paint is null)
            {
                return NotFound();
            }

            updatedPaint.Id = paint.Id;

            await _paintService.UpdateAsync(id, updatedPaint);

            return NoContent();
        }

        [HttpDelete("{id:length(24)}")]
        public async Task<IActionResult> Delete(string id)
        {
            var paint = await _paintService.GetAsync(id);

            if (paint is null)
            {
                return NotFound();
            }

            await _paintService.RemoveAsync(id);

            return NoContent();
        }
    }
}
