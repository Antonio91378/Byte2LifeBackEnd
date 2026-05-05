using Byte2Life.API.Models;
using Byte2Life.API.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Byte2Life.API.Controllers
{
    [ApiController]
    [Route("api/printer-monitor")]
    public class PrinterMonitorController : ControllerBase
    {
        private readonly IPrinterMonitorService _printerMonitorService;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public PrinterMonitorController(
            IPrinterMonitorService printerMonitorService,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            _printerMonitorService = printerMonitorService;
            _configuration = configuration;
            _environment = environment;
        }

        [HttpGet("latest")]
        public async Task<ActionResult<PrinterMonitorStatus>> GetLatest()
        {
            var latest = await _printerMonitorService.GetLatestAsync();
            return latest is null ? NotFound(new { message = "No printer data received yet." }) : latest;
        }

        [HttpGet("history")]
        public async Task<ActionResult<List<PrinterMonitorStatus>>> GetHistory([FromQuery] int limit = 100)
        {
            return await _printerMonitorService.GetHistoryAsync(limit);
        }

        [HttpGet("commands")]
        public async Task<ActionResult<List<PrinterCommand>>> GetCommands([FromQuery] int limit = 30)
        {
            return await _printerMonitorService.GetRecentCommandsAsync(limit);
        }

        [HttpPost("commands")]
        public async Task<ActionResult<PrinterCommand>> CreateCommand([FromBody] PrinterCommandCreateRequest request)
        {
            if (!IsCommandAuthorized())
            {
                return Unauthorized(new { message = "Invalid printer command PIN." });
            }

            try
            {
                var command = await _printerMonitorService.CreateCommandAsync(request);
                return Accepted(command);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("commands/next")]
        public async Task<ActionResult<PrinterCommand>> ClaimNextCommand([FromQuery] string? agentId)
        {
            if (!IsAuthorized())
            {
                return Unauthorized(new { message = "Invalid printer monitor token." });
            }

            var command = await _printerMonitorService.ClaimNextCommandAsync(agentId ?? "local-worker");
            return command is null ? NoContent() : command;
        }

        [HttpPost("commands/{id}/complete")]
        public async Task<ActionResult<PrinterCommand>> CompleteCommand(string id, [FromBody] PrinterCommandCompleteRequest request)
        {
            if (!IsAuthorized())
            {
                return Unauthorized(new { message = "Invalid printer monitor token." });
            }

            var command = await _printerMonitorService.CompleteCommandAsync(id, request);
            return command is null ? NotFound(new { message = "Command not found." }) : command;
        }

        [HttpPost("update")]
        public async Task<ActionResult<PrinterMonitorStatus>> Update([FromBody] PrinterMonitorStatus status)
        {
            if (!IsAuthorized())
            {
                return Unauthorized(new { message = "Invalid printer monitor token." });
            }

            var updated = await _printerMonitorService.UpdateAsync(status);
            return Accepted(updated);
        }

        [HttpGet("camera/status")]
        public async Task<IActionResult> GetCameraStatus()
        {
            var frame = await _printerMonitorService.GetLatestCameraFrameAsync();
            if (frame is null)
            {
                return NotFound(new { message = "No camera frame received yet." });
            }

            return Ok(new
            {
                frame.Serial,
                frame.ReceivedAt,
                frame.ContentType,
                frame.ByteLength
            });
        }

        [HttpGet("camera/latest")]
        public async Task<IActionResult> GetLatestCameraFrame()
        {
            var frame = await _printerMonitorService.GetLatestCameraFrameAsync();
            if (frame is null)
            {
                return NotFound(new { message = "No camera frame received yet." });
            }

            Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
            Response.Headers.Pragma = "no-cache";
            Response.Headers.Expires = "0";

            return File(frame.Bytes, frame.ContentType);
        }

        [HttpPost("camera/frame")]
        [RequestSizeLimit(5_000_000)]
        public async Task<IActionResult> UpdateCameraFrame([FromQuery] string? serial)
        {
            if (!IsAuthorized())
            {
                return Unauthorized(new { message = "Invalid printer monitor token." });
            }

            using var memoryStream = new MemoryStream();
            await Request.Body.CopyToAsync(memoryStream);

            var resolvedSerial = !string.IsNullOrWhiteSpace(serial)
                ? serial
                : Request.Headers["X-Bambu-Serial"].ToString();

            if (string.IsNullOrWhiteSpace(resolvedSerial))
            {
                var latest = await _printerMonitorService.GetLatestAsync();
                resolvedSerial = latest?.Serial ?? "";
            }

            if (string.IsNullOrWhiteSpace(resolvedSerial))
            {
                return BadRequest(new { message = "Camera frame serial is required." });
            }

            try
            {
                var frame = await _printerMonitorService.UpdateCameraFrameAsync(resolvedSerial, memoryStream.ToArray());
                return Accepted(new
                {
                    frame.Serial,
                    frame.ReceivedAt,
                    frame.ContentType,
                    frame.ByteLength
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("events")]
        public async Task Events(CancellationToken cancellationToken)
        {
            Response.Headers.CacheControl = "no-cache, no-transform";
            Response.Headers.Connection = "keep-alive";
            Response.ContentType = "text/event-stream";

            PrinterMonitorStatus? lastSent = null;

            while (!cancellationToken.IsCancellationRequested)
            {
                var latest = await _printerMonitorService.GetLatestAsync();
                if (latest is not null && latest.ReceivedAt != lastSent?.ReceivedAt)
                {
                    var payload = JsonSerializer.Serialize(latest, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                    await Response.WriteAsync($"event: printer\ndata: {payload}\n\n", cancellationToken);
                    await Response.Body.FlushAsync(cancellationToken);
                    lastSent = latest;
                }

                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }

        private bool IsAuthorized()
        {
            var expectedToken = _configuration["PrinterMonitor:IngestToken"] ??
                Environment.GetEnvironmentVariable("BAMBU_INGEST_TOKEN");

            if (string.IsNullOrWhiteSpace(expectedToken))
            {
                return true;
            }

            var authorization = Request.Headers.Authorization.ToString();
            return string.Equals(authorization, $"Bearer {expectedToken}", StringComparison.Ordinal);
        }

        private bool IsCommandAuthorized()
        {
            var expectedPin = _configuration["PrinterMonitor:CommandPin"] ??
                Environment.GetEnvironmentVariable("BAMBU_COMMAND_PIN");

            if (string.IsNullOrWhiteSpace(expectedPin))
            {
                return _environment.IsDevelopment();
            }

            var suppliedPin = Request.Headers["X-Printer-Command-Pin"].ToString();
            return string.Equals(suppliedPin, expectedPin, StringComparison.Ordinal);
        }
    }
}
