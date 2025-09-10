using System;
using System.ComponentModel.DataAnnotations;

namespace NDAProcesses.Shared.Models
{
    public class MessageModel
    {
        [Key]
        public int Id { get; set; }
        public string? ExternalId { get; set; }
        public string? UserName { get; set; }
        public string? Sender { get; set; }
        public string Recipient { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
        public string Direction { get; set; }
    }
}
