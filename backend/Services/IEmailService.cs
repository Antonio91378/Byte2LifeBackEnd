namespace Byte2Life.API.Services
{
    public interface IEmailService
    {
        bool IsConfigured { get; }
        Task SendAsync(IEnumerable<string> recipients, string subject, string body);
    }
}
