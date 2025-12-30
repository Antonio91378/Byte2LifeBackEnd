using Byte2Life.API.Models;
using Byte2Life.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Byte2Life.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvestmentsController : ControllerBase
    {
        private readonly IInvestmentService _service;

        public InvestmentsController(IInvestmentService service)
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
        public async Task<IActionResult> Create(Investment investment)
        {
            await _service.CreateAsync(investment);
            return CreatedAtAction(nameof(GetById), new { id = investment.Id?.ToString() }, investment);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, Investment investment)
        {
            await _service.UpdateAsync(id, investment);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }

        [HttpGet("total")]
        public async Task<IActionResult> GetTotal()
        {
            return Ok(await _service.GetTotalInvestmentAsync());
        }
    }
}
