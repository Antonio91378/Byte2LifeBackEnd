using Byte2Life.API.Models;
using Byte2Life.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Byte2Life.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RemindersController : ControllerBase
    {
        private readonly IReminderService _service;

        public RemindersController(IReminderService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _service.GetAllAsync());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Reminder reminder)
        {
            await _service.CreateAsync(reminder);
            return CreatedAtAction(nameof(GetById), new { id = reminder.Id?.ToString() }, reminder);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, Reminder reminder)
        {
            var existing = await _service.GetByIdAsync(id);
            if (existing == null) return NotFound();

            reminder.Id = existing.Id;
            reminder.CreatedAt = existing.CreatedAt;
            await _service.UpdateAsync(id, reminder);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }
    }
}
