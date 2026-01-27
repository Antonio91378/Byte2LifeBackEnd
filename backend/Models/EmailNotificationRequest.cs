namespace Byte2Life.API.Models
{
    public class EmailNotificationRequest
    {
        public List<string> To { get; set; } = new();
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
    }
}
