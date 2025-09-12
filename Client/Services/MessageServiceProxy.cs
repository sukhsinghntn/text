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
        private List<ContactModel>? _contacts;
        private readonly Dictionary<string, List<MessageModel>> _messages = new();
        private readonly Dictionary<string, Dictionary<string, DateTime>> _readStates = new();

        public MessageServiceProxy(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<MessageModel>> GetMessages(string userName)
        {
            if (_messages.TryGetValue(userName, out var cached))
                return cached;

            var data = await _httpClient.GetFromJsonAsync<IEnumerable<MessageModel>>($"api/messages/{userName}")
                ?? Enumerable.Empty<MessageModel>();
            var list = data.ToList();
            _messages[userName] = list;
            return list;
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
            var response = await _httpClient.PostAsJsonAsync("api/messages", message);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new ApplicationException(string.IsNullOrWhiteSpace(error) ? "Failed to send message" : error);
            }
            _messages.Remove(message.Sender);
        }

        public async Task<IEnumerable<ContactModel>> GetContacts()
        {
            if (_contacts != null)
                return _contacts;

            var data = await _httpClient.GetFromJsonAsync<IEnumerable<ContactModel>>("api/messages/contacts")
                ?? Enumerable.Empty<ContactModel>();
            _contacts = data.ToList();
            return _contacts;
        }

        public async Task SaveContact(ContactModel contact)
        {
            var response = await _httpClient.PostAsJsonAsync("api/messages/contacts", contact);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new ApplicationException(string.IsNullOrWhiteSpace(error) ? "Failed to save contact" : error);
            }
            _contacts = null;
        }

        public async Task DeleteContact(int id)
        {
            await _httpClient.DeleteAsync($"api/messages/contacts/{id}");
            _contacts = null;
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

        public async Task<Dictionary<string, DateTime>> GetReadStates(string userName)
        {
            if (_readStates.TryGetValue(userName, out var cached))
                return cached;

            var data = await _httpClient.GetFromJsonAsync<Dictionary<string, DateTime>>($"api/messages/{userName}/readstates")
                ?? new Dictionary<string, DateTime>();
            _readStates[userName] = data;
            return data;
        }

        public async Task MarkRead(string userName, string recipient, DateTime timestamp)
        {
            var payload = new { Recipient = recipient, Timestamp = timestamp };
            await _httpClient.PostAsJsonAsync($"api/messages/{userName}/read", payload);
            if (_readStates.TryGetValue(userName, out var rs))
                rs[recipient] = timestamp;
        }

        public async Task PreloadAsync(string userName)
        {
            var contactsTask = _httpClient.GetFromJsonAsync<IEnumerable<ContactModel>>("api/messages/contacts");
            var messagesTask = _httpClient.GetFromJsonAsync<IEnumerable<MessageModel>>($"api/messages/{userName}");
            var readTask = _httpClient.GetFromJsonAsync<Dictionary<string, DateTime>>($"api/messages/{userName}/readstates");
            await Task.WhenAll(contactsTask, messagesTask, readTask);

            _contacts = contactsTask.Result?.ToList() ?? new List<ContactModel>();
            _messages[userName] = messagesTask.Result?.ToList() ?? new List<MessageModel>();
            _readStates[userName] = readTask.Result ?? new Dictionary<string, DateTime>();
        }
    }
}
