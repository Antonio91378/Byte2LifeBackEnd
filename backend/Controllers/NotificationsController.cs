using Byte2Life.API.Models;
using Byte2Life.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Byte2Life.API.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    public class NotificationsController : ControllerBase
    {
        private readonly IEmailService _emailService;

        public NotificationsController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        [HttpPost("email")]
        public async Task<IActionResult> SendEmail([FromBody] EmailNotificationRequest request)
        {
            if (request == null || request.To.Count == 0)
            {
                return BadRequest("Informe destinatarios validos.");
            }

            if (!_emailService.IsConfigured)
            {
                return StatusCode(StatusCodes.Status501NotImplemented, "Email nao configurado.");
            }

            try
            {
                await _emailService.SendAsync(request.To, request.Subject, request.Body);
                return Ok(new { sent = true });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
