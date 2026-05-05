using Byte2Life.API.Models;
using Byte2Life.API.Persistence;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.Json;

namespace Byte2Life.API.Services
{
    public class PrinterMonitorService : IPrinterMonitorService
    {
        private readonly IMongoCollection<PrinterMonitorStatus> _historyCollection;
        private readonly IMongoCollection<PrinterCommand> _commandsCollection;
        private PrinterMonitorStatus? _latest;
        private PrinterCameraFrame? _latestCameraFrame;

        public PrinterMonitorService(IMongoDatabase database)
        {
            _historyCollection = database.GetCollection<PrinterMonitorStatus>(MongoCollectionNames.PrinterMonitorHistory);
            _commandsCollection = database.GetCollection<PrinterCommand>(MongoCollectionNames.PrinterCommands);
        }

        public async Task<PrinterMonitorStatus?> GetLatestAsync()
        {
            if (_latest is not null)
            {
                return _latest;
            }

            _latest = await _historyCollection
                .Find(FilterDefinition<PrinterMonitorStatus>.Empty)
                .SortByDescending(status => status.ReceivedAt)
                .FirstOrDefaultAsync();

            return _latest;
        }

        public async Task<List<PrinterMonitorStatus>> GetHistoryAsync(int limit = 100)
        {
            var normalizedLimit = Math.Clamp(limit, 1, 500);
            return await _historyCollection
                .Find(FilterDefinition<PrinterMonitorStatus>.Empty)
                .SortByDescending(status => status.ReceivedAt)
                .Limit(normalizedLimit)
                .ToListAsync();
        }

        public async Task<PrinterMonitorStatus> UpdateAsync(PrinterMonitorStatus status)
        {
            status.Id = ObjectId.GenerateNewId();
            status.ReceivedAt = status.ReceivedAt == default ? DateTime.UtcNow : status.ReceivedAt.ToUniversalTime();
            status.Events = DeriveEvents(status);
            status.RawPrint = null;

            _latest = status;
            await _historyCollection.InsertOneAsync(status);

            return status;
        }

        public Task<PrinterCameraFrame?> GetLatestCameraFrameAsync()
        {
            return Task.FromResult(_latestCameraFrame);
        }

        public Task<PrinterCameraFrame> UpdateCameraFrameAsync(string serial, byte[] jpegBytes, DateTime? receivedAt = null)
        {
            if (jpegBytes.Length < 4 ||
                jpegBytes[0] != 0xff ||
                jpegBytes[1] != 0xd8 ||
                jpegBytes[^2] != 0xff ||
                jpegBytes[^1] != 0xd9)
            {
                throw new InvalidOperationException("Camera frame must be a complete JPEG image.");
            }

            var frame = new PrinterCameraFrame
            {
                Serial = serial,
                ReceivedAt = (receivedAt ?? DateTime.UtcNow).ToUniversalTime(),
                Bytes = jpegBytes
            };

            _latestCameraFrame = frame;
            return Task.FromResult(frame);
        }

        public async Task<PrinterCommand> CreateCommandAsync(PrinterCommandCreateRequest request)
        {
            var latest = await GetLatestAsync();
            var serial = !string.IsNullOrWhiteSpace(request.Serial)
                ? request.Serial.Trim()
                : latest?.Serial ?? "";

            if (string.IsNullOrWhiteSpace(serial))
            {
                throw new InvalidOperationException("No printer serial is available for command creation.");
            }

            var command = BuildCommand(serial, request);
            await _commandsCollection.InsertOneAsync(command);
            return command;
        }

        public async Task<List<PrinterCommand>> GetRecentCommandsAsync(int limit = 30)
        {
            var normalizedLimit = Math.Clamp(limit, 1, 100);
            return await _commandsCollection
                .Find(FilterDefinition<PrinterCommand>.Empty)
                .SortByDescending(command => command.CreatedAt)
                .Limit(normalizedLimit)
                .ToListAsync();
        }

        public async Task<PrinterCommand?> ClaimNextCommandAsync(string agentId)
        {
            var normalizedAgentId = string.IsNullOrWhiteSpace(agentId) ? "local-worker" : agentId.Trim();
            var filter = Builders<PrinterCommand>.Filter.Eq(command => command.Status, "pending");
            var update = Builders<PrinterCommand>.Update
                .Set(command => command.Status, "claimed")
                .Set(command => command.ClaimedBy, normalizedAgentId)
                .Set(command => command.ClaimedAt, DateTime.UtcNow);

            return await _commandsCollection.FindOneAndUpdateAsync(
                filter,
                update,
                new FindOneAndUpdateOptions<PrinterCommand>
                {
                    Sort = Builders<PrinterCommand>.Sort.Ascending(command => command.CreatedAt),
                    ReturnDocument = ReturnDocument.After
                });
        }

        public async Task<PrinterCommand?> CompleteCommandAsync(string commandId, PrinterCommandCompleteRequest request)
        {
            if (!ObjectId.TryParse(commandId, out var objectId))
            {
                return null;
            }

            var update = Builders<PrinterCommand>.Update
                .Set(command => command.Status, request.Success ? "succeeded" : "failed")
                .Set(command => command.CompletedAt, DateTime.UtcNow)
                .Set(command => command.Error, request.Success ? null : request.Error ?? "Command failed");

            return await _commandsCollection.FindOneAndUpdateAsync(
                MongoId.FilterById<PrinterCommand>(objectId),
                update,
                new FindOneAndUpdateOptions<PrinterCommand> { ReturnDocument = ReturnDocument.After });
        }

        private static PrinterCommand BuildCommand(string serial, PrinterCommandCreateRequest request)
        {
            var sequenceId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            var type = request.Type.Trim().ToLowerInvariant();

            return type switch
            {
                "chamber_light" => BuildJsonCommand(
                    serial,
                    type,
                    request.Mode == "on" ? "Ligar luz da impressora" : "Desligar luz da impressora",
                    new
                    {
                        system = new
                        {
                            sequence_id = sequenceId,
                            command = "ledctrl",
                            led_node = "chamber_light",
                            led_mode = request.Mode == "on" ? "on" : "off",
                            led_on_time = 500,
                            led_off_time = 500,
                            loop_times = 0,
                            interval_time = 0
                        }
                    }),

                "pause_print" => BuildJsonCommand(serial, type, "Pausar impressao", new
                {
                    print = new { sequence_id = sequenceId, command = "pause" }
                }),

                "resume_print" => BuildJsonCommand(serial, type, "Retomar impressao", new
                {
                    print = new { sequence_id = sequenceId, command = "resume" }
                }),

                "stop_print" => BuildJsonCommand(serial, type, "Parar impressao", new
                {
                    print = new { sequence_id = sequenceId, command = "stop" }
                }),

                "bed_temp" => BuildGcodeCommand(serial, type, $"Mesa para {NormalizeTemperature(request.Value, 0, 110)} C", sequenceId, $"M140 S{NormalizeTemperature(request.Value, 0, 110)}"),
                "nozzle_temp" => BuildGcodeCommand(serial, type, $"Bico para {NormalizeTemperature(request.Value, 0, 300)} C", sequenceId, $"M104 S{NormalizeTemperature(request.Value, 0, 300)}"),

                "speed_profile" => BuildJsonCommand(serial, type, $"Velocidade perfil {NormalizeSpeedProfile(request.Mode)}", new
                {
                    print = new
                    {
                        sequence_id = sequenceId,
                        command = "print_speed",
                        param = NormalizeSpeedProfile(request.Mode)
                    }
                }),

                "custom_gcode" => BuildGcodeCommand(serial, type, "Executar G-code customizado", sequenceId, NormalizeGcode(request.Gcode)),

                _ => throw new InvalidOperationException($"Unsupported printer command type: {request.Type}")
            };
        }

        private static PrinterCommand BuildGcodeCommand(string serial, string type, string label, string sequenceId, string gcode) =>
            BuildJsonCommand(serial, type, label, new
            {
                print = new
                {
                    sequence_id = sequenceId,
                    command = "gcode_line",
                    param = gcode
                }
            });

        private static PrinterCommand BuildJsonCommand(string serial, string type, string label, object payload) =>
            new()
            {
                ObjectId = ObjectId.GenerateNewId(),
                Serial = serial,
                Type = type,
                Label = label,
                Status = "pending",
                PayloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                CreatedAt = DateTime.UtcNow
            };

        private static int NormalizeTemperature(double? value, int min, int max)
        {
            if (value is null || double.IsNaN(value.Value))
            {
                throw new InvalidOperationException("Temperature value is required.");
            }

            return Math.Clamp((int)Math.Round(value.Value), min, max);
        }

        private static string NormalizeSpeedProfile(string? mode)
        {
            var profile = mode?.Trim() switch
            {
                "1" or "silent" => "1",
                "2" or "standard" => "2",
                "3" or "sport" => "3",
                "4" or "ludicrous" => "4",
                _ => "2"
            };

            return profile;
        }

        private static string NormalizeGcode(string? gcode)
        {
            var normalized = (gcode ?? "").Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                throw new InvalidOperationException("G-code is required.");
            }

            if (normalized.Length > 500)
            {
                throw new InvalidOperationException("G-code command is too long.");
            }

            return normalized;
        }

        private static List<PrinterMonitorEvent> DeriveEvents(PrinterMonitorStatus status)
        {
            var events = new List<PrinterMonitorEvent>();
            var job = status.Summary.Job;
            var health = status.Summary.Health;
            var temperatures = status.Summary.Temperatures;

            if (health.PrintError is > 0)
            {
                events.Add(new PrinterMonitorEvent
                {
                    Level = "error",
                    Message = $"Erro da impressora: {health.PrintError}"
                });
            }

            if (!string.IsNullOrWhiteSpace(health.FailReason))
            {
                events.Add(new PrinterMonitorEvent
                {
                    Level = "error",
                    Message = $"Falha: {health.FailReason}"
                });
            }

            if (!string.Equals(job.State, "FINISH", StringComparison.OrdinalIgnoreCase) &&
                job.ProgressPercent is >= 95)
            {
                events.Add(new PrinterMonitorEvent
                {
                    Level = "info",
                    Message = "Impressao quase finalizada"
                });
            }

            if (temperatures.NozzleC is > 260)
            {
                events.Add(new PrinterMonitorEvent
                {
                    Level = "warning",
                    Message = "Bico acima de 260 C"
                });
            }

            return events;
        }
    }
}
