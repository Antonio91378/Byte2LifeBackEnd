using ServiceProviderModel = Byte2Life.API.Models.ServiceProvider;
using Byte2Life.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Byte2Life.API.Controllers
{
    [ApiController]
    [Route("api/service-providers")]
    public class ServiceProvidersController : ControllerBase
    {
        private readonly IServiceProviderService _service;

        public ServiceProvidersController(IServiceProviderService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<List<ServiceProviderModel>> GetAll() =>
            await _service.GetAsync();

        [HttpGet("{id:length(24)}")]
        public async Task<ActionResult<ServiceProviderModel>> GetById(string id)
        {
            var item = await _service.GetAsync(id);
            if (item is null)
            {
                return NotFound();
            }

            return item;
        }

        [HttpPost]
        public async Task<IActionResult> Create(ServiceProviderModel provider)
        {
            await _service.CreateAsync(provider);
            return CreatedAtAction(nameof(GetById), new { id = provider.Id?.ToString() }, provider);
        }

        [HttpPut("{id:length(24)}")]
        public async Task<IActionResult> Update(string id, ServiceProviderModel provider)
        {
            var existing = await _service.GetAsync(id);
            if (existing is null)
            {
                return NotFound();
            }

            provider.Id = existing.Id;
            await _service.UpdateAsync(id, provider);
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
