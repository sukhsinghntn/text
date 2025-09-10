using NDAProcesses.Shared.Models;

namespace NDAProcesses.Shared.Services
{
    public interface IMessageService
    {
        Task<IEnumerable<MessageModel>> GetMessages(string userName);
        Task<IEnumerable<MessageModel>> GetConversation(string userName, string recipient);
        Task<IEnumerable<string>> GetRecipients(string userName);
        Task SendMessage(MessageModel message);
        Task<IEnumerable<ContactModel>> GetContacts(string userName);
        Task SaveContact(ContactModel contact);
        Task DeleteContact(int id, string userName);
        Task ScheduleMessage(ScheduledMessageModel message);
        Task<IEnumerable<ScheduledMessageModel>> GetScheduledMessages(string userName);
        Task CancelScheduledMessage(int id, string userName);
        Task SyncInbox();
    }
}
