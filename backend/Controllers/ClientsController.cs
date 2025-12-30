using Byte2Life.API.Models;
using Byte2Life.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Byte2Life.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientsController : ControllerBase
    {
        private readonly IClientService _clientService;

        public ClientsController(IClientService clientService) =>
            _clientService = clientService;

        [HttpGet]
        public async Task<List<Client>> Get([FromQuery] string? name = null) =>
            await _clientService.GetAsync(name);

        [HttpGet("{id:length(24)}")]
        public async Task<ActionResult<Client>> GetById(string id)
        {
            var client = await _clientService.GetByIdAsync(id);

            if (client is null)
            {
                return NotFound();
            }

            return client;
        }

        [HttpPost]
        public async Task<IActionResult> Post(Client newClient)
        {
            await _clientService.CreateAsync(newClient);

            return CreatedAtAction(nameof(Get), new { id = newClient.Id }, newClient);
        }

        [HttpPut("{id:length(24)}")]
        public async Task<IActionResult> Update(string id, Client updatedClient)
        {
            var client = await _clientService.GetByIdAsync(id);

            if (client is null)
            {
                return NotFound();
            }

            updatedClient.Id = client.Id;

            await _clientService.UpdateAsync(id, updatedClient);

            return NoContent();
        }

        [HttpDelete("{id:length(24)}")]
        public async Task<IActionResult> Delete(string id)
        {
            var client = await _clientService.GetByIdAsync(id);

            if (client is null)
            {
                return NotFound();
            }

            try
            {
                await _clientService.RemoveAsync(id);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }

            return NoContent();
        }
    }
}
