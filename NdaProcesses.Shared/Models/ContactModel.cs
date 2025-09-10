namespace NDAProcesses.Shared.Models
{
    public class ContactModel
    {
        public int Id { get; set; }
        public string Owner { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
    }
}
