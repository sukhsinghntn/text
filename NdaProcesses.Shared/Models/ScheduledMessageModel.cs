namespace NDAProcesses.Shared.Models
{
    public class ScheduledMessageModel
    {
        public int Id { get; set; }
        public string Sender { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string SenderDepartment { get; set; } = string.Empty;
        public string Recipient { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public DateTime ScheduledFor { get; set; }
        public bool Sent { get; set; }
    }
}
