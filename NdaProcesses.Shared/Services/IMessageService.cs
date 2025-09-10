using NDAProcesses.Shared.Models;

namespace NDAProcesses.Shared.Services
{
    public interface IMessageService
    {
        Task<IEnumerable<MessageModel>> GetMessages(string userName);
        Task SendMessage(MessageModel message);
        Task SyncInbox();
    }
}
