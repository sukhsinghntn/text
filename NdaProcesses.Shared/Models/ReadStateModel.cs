using System;

namespace NDAProcesses.Shared.Models
{
    public class ReadStateModel
    {
        public int Id { get; set; }
        public string Department { get; set; } = string.Empty;
        public string Recipient { get; set; } = string.Empty;
        public DateTime LastRead { get; set; }
    }
}
