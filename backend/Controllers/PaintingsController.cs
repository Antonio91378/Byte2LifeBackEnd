using Byte2Life.API.Models;
using Byte2Life.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Byte2Life.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaintingsController : ControllerBase
    {
        private readonly IPaintingTaskService _service;

        public PaintingsController(IPaintingTaskService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _service.GetAllAsync());
        }

        [HttpGet("{id:length(24)}")]
        public async Task<IActionResult> GetById(string id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> Create(PaintingTask task)
        {
            await _service.CreateAsync(task);
            return CreatedAtAction(nameof(GetById), new { id = task.Id?.ToString() }, task);
        }

        [HttpPut("{id:length(24)}")]
        public async Task<IActionResult> Update(string id, PaintingTask task)
        {
            var existing = await _service.GetByIdAsync(id);
            if (existing == null) return NotFound();

            task.Id = existing.Id;
            task.CreatedAt = existing.CreatedAt;
            await _service.UpdateAsync(id, task);
            return NoContent();
        }

        [HttpDelete("{id:length(24)}")]
        public async Task<IActionResult> Delete(string id)
        {
            await _service.RemoveAsync(id);
            return NoContent();
        }
    }
}
