using NDAProcesses.Shared.Models;
using NDAProcesses.Shared.Services;
using System.Net.Http.Json;
using System.Collections.Generic;
using System.Linq;

namespace NDAProcesses.Client.Services
{
    public class MessageServiceProxy : IMessageService
    {
        private readonly HttpClient _httpClient;

        public MessageServiceProxy(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<MessageModel>> GetMessages(string userName)
        {
            return await _httpClient.GetFromJsonAsync<IEnumerable<MessageModel>>($"api/messages/{userName}")
                ?? Enumerable.Empty<MessageModel>();
        }

        public async Task SendMessage(MessageModel message)
        {
            await _httpClient.PostAsJsonAsync("api/messages", message);
        }

        public Task SyncInbox()
        {
            // Inbox is automatically synced server-side
            return Task.CompletedTask;
        }
    }
}
