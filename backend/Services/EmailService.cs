using System.Net;
using System.Net.Mail;
using Byte2Life.API.Models;
using Microsoft.Extensions.Options;

namespace Byte2Life.API.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;

        public EmailService(IOptions<EmailSettings> settings)
        {
            _settings = settings.Value;
        }

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(_settings.Host) &&
            !string.IsNullOrWhiteSpace(_settings.From);

        public async Task SendAsync(IEnumerable<string> recipients, string subject, string body)
        {
            if (!IsConfigured)
            {
                throw new InvalidOperationException("Email settings are not configured.");
            }

            var uniqueRecipients = recipients
                .Select(email => email?.Trim())
                .Where(email => !string.IsNullOrWhiteSpace(email))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (uniqueRecipients.Count == 0)
            {
                throw new InvalidOperationException("No valid recipients provided.");
            }

            using var message = new MailMessage
            {
                From = new MailAddress(_settings.From, _settings.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };

            foreach (var recipient in uniqueRecipients)
            {
                message.To.Add(recipient);
            }

            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = _settings.EnableSsl
            };

            if (!string.IsNullOrWhiteSpace(_settings.Username))
            {
                client.Credentials = new NetworkCredential(_settings.Username, _settings.Password);
            }

            await client.SendMailAsync(message);
        }
    }
}
