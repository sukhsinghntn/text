using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using NDAProcesses.Shared.Models;
using NDAProcesses.Shared.Services;

namespace NDAProcesses.Client.Services
{
    public class MessageServiceProxy : IMessageService
    {
        private readonly HttpClient _httpClient;
        public MessageServiceProxy(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<MessageModel>> GetMessages(string userName)
        {
            return await _httpClient.GetFromJsonAsync<List<MessageModel>>($"api/messages/{userName}")
                ?? new List<MessageModel>();
        }

        public async Task SendMessage(MessageModel message)
        {
            await _httpClient.PostAsJsonAsync("api/messages/send", message);
        }
    }
}
