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

        public async Task<IEnumerable<MessageModel>> GetConversation(string userName, string recipient)
        {
            return await _httpClient.GetFromJsonAsync<IEnumerable<MessageModel>>($"api/messages/{userName}/conversation/{recipient}")
                ?? Enumerable.Empty<MessageModel>();
        }

        public async Task<IEnumerable<string>> GetRecipients(string userName)
        {
            return await _httpClient.GetFromJsonAsync<IEnumerable<string>>($"api/messages/{userName}/recipients")
                ?? Enumerable.Empty<string>();
        }

        public async Task SendMessage(MessageModel message)
        {
            await _httpClient.PostAsJsonAsync("api/messages", message);
        }

        public async Task<IEnumerable<ContactModel>> GetContacts()
        {
            return await _httpClient.GetFromJsonAsync<IEnumerable<ContactModel>>("api/messages/contacts")
                ?? Enumerable.Empty<ContactModel>();
        }

        public async Task SaveContact(ContactModel contact)
        {
            await _httpClient.PostAsJsonAsync("api/messages/contacts", contact);
        }

        public async Task DeleteContact(int id)
        {
            await _httpClient.DeleteAsync($"api/messages/contacts/{id}");
        }

        public async Task ScheduleMessage(ScheduledMessageModel message)
        {
            await _httpClient.PostAsJsonAsync("api/messages/schedule", message);
        }

        public async Task<IEnumerable<ScheduledMessageModel>> GetScheduledMessages(string userName)
        {
            return await _httpClient.GetFromJsonAsync<IEnumerable<ScheduledMessageModel>>($"api/messages/{userName}/scheduled")
                ?? Enumerable.Empty<ScheduledMessageModel>();
        }

        public async Task CancelScheduledMessage(int id, string userName)
        {
            await _httpClient.DeleteAsync($"api/messages/{userName}/scheduled/{id}");
        }

        public Task SyncInbox()
        {
            // Inbox is automatically synced server-side
            return Task.CompletedTask;
        }
    }
}
