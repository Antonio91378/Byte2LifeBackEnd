using Byte2Life.API.Models;

namespace Byte2Life.API.Services
{
    public interface IPrinterMonitorService
    {
        Task<PrinterMonitorStatus?> GetLatestAsync();
        Task<List<PrinterMonitorStatus>> GetHistoryAsync(int limit = 100);
        Task<PrinterMonitorStatus> UpdateAsync(PrinterMonitorStatus status);
        Task<PrinterCameraFrame?> GetLatestCameraFrameAsync();
        Task<PrinterCameraFrame> UpdateCameraFrameAsync(string serial, byte[] jpegBytes, DateTime? receivedAt = null);
        Task<PrinterCommand> CreateCommandAsync(PrinterCommandCreateRequest request);
        Task<List<PrinterCommand>> GetRecentCommandsAsync(int limit = 30);
        Task<PrinterCommand?> ClaimNextCommandAsync(string agentId);
        Task<PrinterCommand?> CompleteCommandAsync(string commandId, PrinterCommandCompleteRequest request);
    }
}
