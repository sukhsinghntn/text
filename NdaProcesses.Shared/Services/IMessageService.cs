using System.Collections.Generic;
using System.Threading.Tasks;
using NDAProcesses.Shared.Models;

namespace NDAProcesses.Shared.Services
{
    public interface IMessageService
    {
        Task<List<MessageModel>> GetMessages(string userName);
        Task SendMessage(MessageModel message);
    }
}
