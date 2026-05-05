using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Byte2Life.API.Models
{
    public class PrinterMonitorStatus
    {
        [BsonId]
        [JsonIgnore]
        public ObjectId Id { get; set; }

        public string Host { get; set; } = "";
        public int Port { get; set; }
        public string Serial { get; set; } = "";

        [JsonPropertyName("received_at")]
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

        public PrinterMonitorSummary Summary { get; set; } = new();
        public List<PrinterMonitorEvent> Events { get; set; } = [];

        [BsonIgnore]
        [JsonPropertyName("raw_print")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public JsonElement? RawPrint { get; set; }
    }

    public class PrinterMonitorSummary
    {
        public PrinterMonitorDevice Device { get; set; } = new();
        public PrinterMonitorJob Job { get; set; } = new();
        public PrinterMonitorTemperatures Temperatures { get; set; } = new();
        public PrinterMonitorMaterial Material { get; set; } = new();
        public PrinterMonitorHealth Health { get; set; } = new();

        [JsonPropertyName("raw_keys")]
        public List<string> RawKeys { get; set; } = [];
    }

    public class PrinterMonitorDevice
    {
        public string Serial { get; set; } = "";
        public string? Name { get; set; }
        public string? Model { get; set; }

        [BsonIgnore]
        public JsonElement? Online { get; set; }

        [JsonPropertyName("wifi_signal")]
        public string? WifiSignal { get; set; }
    }

    public class PrinterMonitorJob
    {
        public string? State { get; set; }
        public string? File { get; set; }

        [JsonPropertyName("progress_percent")]
        public double? ProgressPercent { get; set; }

        [JsonPropertyName("remaining_minutes")]
        public double? RemainingMinutes { get; set; }

        public int? Layer { get; set; }

        [JsonPropertyName("total_layers")]
        public int? TotalLayers { get; set; }

        public int? Stage { get; set; }
    }

    public class PrinterMonitorTemperatures
    {
        [JsonPropertyName("nozzle_c")]
        public double? NozzleC { get; set; }

        [JsonPropertyName("nozzle_target_c")]
        public double? NozzleTargetC { get; set; }

        [JsonPropertyName("bed_c")]
        public double? BedC { get; set; }

        [JsonPropertyName("bed_target_c")]
        public double? BedTargetC { get; set; }

        [JsonPropertyName("chamber_c")]
        public double? ChamberC { get; set; }
    }

    public class PrinterMonitorMaterial
    {
        [JsonPropertyName("tray_now")]
        public int? TrayNow { get; set; }

        [JsonPropertyName("ams_status")]
        public int? AmsStatus { get; set; }

        [JsonPropertyName("vt_tray")]
        public PrinterMonitorTray? VtTray { get; set; }
    }

    public class PrinterMonitorTray
    {
        [JsonPropertyName("tray_type")]
        public string? TrayType { get; set; }

        [JsonPropertyName("tray_color")]
        public string? TrayColor { get; set; }

        [JsonPropertyName("tray_info_idx")]
        public string? TrayInfoIdx { get; set; }
    }

    public class PrinterMonitorHealth
    {
        [JsonPropertyName("print_error")]
        public int? PrintError { get; set; }

        [JsonPropertyName("fail_reason")]
        public string? FailReason { get; set; }

        [BsonIgnore]
        public JsonElement? Hms { get; set; }
    }

    public class PrinterMonitorEvent
    {
        public string Level { get; set; } = "info";
        public string Message { get; set; } = "";
    }

    public class PrinterCameraFrame
    {
        public string Serial { get; set; } = "";
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
        public string ContentType { get; set; } = "image/jpeg";
        public byte[] Bytes { get; set; } = [];
        public int ByteLength => Bytes.Length;
    }

    public class PrinterCommand
    {
        [BsonId]
        [JsonIgnore]
        public ObjectId ObjectId { get; set; }

        [BsonIgnore]
        public string Id => ObjectId.ToString();

        public string Serial { get; set; } = "";
        public string Type { get; set; } = "";
        public string Label { get; set; } = "";
        public string Status { get; set; } = "pending";
        public string PayloadJson { get; set; } = "";
        public string? Error { get; set; }
        public string? ClaimedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ClaimedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class PrinterCommandCreateRequest
    {
        public string Type { get; set; } = "";
        public string? Mode { get; set; }
        public double? Value { get; set; }
        public string? Gcode { get; set; }
        public string? Serial { get; set; }
    }

    public class PrinterCommandCompleteRequest
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
    }
}
