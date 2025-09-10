namespace NDAProcesses.Shared.Models
{
    public class MessageModel
    {
        public int Id { get; set; }
        public string Sender { get; set; } = string.Empty;
        public string Recipient { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Direction { get; set; } = string.Empty;
    }
}
