using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using NDAProcesses.Server.Data;
using NDAProcesses.Shared.Models;
using NDAProcesses.Shared.Services;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Linq;

namespace NDAProcesses.Server.Services
{
    public class MessageService : IMessageService
    {
        private readonly MessageContext _context;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MessageService> _logger;

        public MessageService(MessageContext context, IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<MessageService> logger)
        {
            _context = context;
            _httpClient = httpClientFactory.CreateClient();
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<IEnumerable<MessageModel>> GetMessages(string userName)
        {
            await _context.Database.EnsureCreatedAsync();
            await SyncInbox();

            var sentRecipients = _context.Messages
                .Where(m => m.Sender == userName)
                .Select(m => m.Recipient);

            return await _context.Messages
                .Where(m => m.Sender == userName || (sentRecipients.Contains(m.Sender) && m.Direction != "Sent"))
                .OrderBy(m => m.Timestamp)
                .ToListAsync();
        }

        public async Task<IEnumerable<MessageModel>> GetConversation(string userName, string recipient)
        {
            await _context.Database.EnsureCreatedAsync();
            await SyncInbox();

            return await _context.Messages
                .Where(m => (m.Sender == userName && m.Recipient == recipient) ||
                            (m.Sender == recipient && m.Recipient == userName))
                .OrderBy(m => m.Timestamp)
                .ToListAsync();
        }

        public async Task<IEnumerable<string>> GetRecipients(string userName)
        {
            await _context.Database.EnsureCreatedAsync();
            return await _context.Messages
                .Where(m => m.Sender == userName || m.Recipient == userName)
                .Select(m => m.Sender == userName ? m.Recipient : m.Sender)
                .Distinct()
                .ToListAsync();
        }

        public async Task SendMessage(MessageModel message)
        {
            await _context.Database.EnsureCreatedAsync();
            var baseUrl = _configuration["TextBee:BaseUrl"];
            var deviceId = _configuration["TextBee:DeviceId"];
            var apiKey = _configuration["TextBee:ApiKey"];
            var url = $"{baseUrl}/gateway/devices/{deviceId}/send-sms";

            var signature = ($"{message.SenderName}{(string.IsNullOrWhiteSpace(message.SenderDepartment) ? string.Empty : " - " + message.SenderDepartment)}").Trim();
            if (!string.IsNullOrWhiteSpace(signature))
            {
                message.Body = $"{message.Body}\n\n{signature}";
            }

            var payload = new
            {
                recipients = new[] { message.Recipient },
                message = message.Body
            };

            var json = JsonSerializer.Serialize(payload);
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("x-api-key", apiKey);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Sending SMS via TextBee to {Recipient}", message.Recipient);
            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("TextBee send failed: {Status} {Body}", response.StatusCode, responseBody);
                throw new HttpRequestException($"TextBee send failed: {response.StatusCode}");
            }

            _logger.LogInformation("TextBee send response: {Body}", responseBody);

            message.Direction = "Sent";
            message.Timestamp = DateTime.UtcNow;
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<ContactModel>> GetContacts(string userName)
        {
            await _context.Database.EnsureCreatedAsync();
            return await _context.Contacts
                .Where(c => c.Owner == userName)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task SaveContact(ContactModel contact)
        {
            await _context.Database.EnsureCreatedAsync();
            if (contact.Id == 0)
            {
                _context.Contacts.Add(contact);
            }
            else
            {
                _context.Contacts.Update(contact);
            }
            await _context.SaveChangesAsync();
        }

        public async Task DeleteContact(int id, string userName)
        {
            await _context.Database.EnsureCreatedAsync();
            var contact = await _context.Contacts.FirstOrDefaultAsync(c => c.Id == id && c.Owner == userName);
            if (contact != null)
            {
                _context.Contacts.Remove(contact);
                await _context.SaveChangesAsync();
            }
        }

        public async Task ScheduleMessage(ScheduledMessageModel message)
        {
            await _context.Database.EnsureCreatedAsync();
            _context.ScheduledMessages.Add(message);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<ScheduledMessageModel>> GetScheduledMessages(string userName)
        {
            await _context.Database.EnsureCreatedAsync();
            return await _context.ScheduledMessages
                .Where(m => m.Sender == userName && !m.Sent)
                .OrderBy(m => m.ScheduledFor)
                .ToListAsync();
        }

        public async Task CancelScheduledMessage(int id, string userName)
        {
            await _context.Database.EnsureCreatedAsync();
            var msg = await _context.ScheduledMessages
                .FirstOrDefaultAsync(m => m.Id == id && m.Sender == userName && !m.Sent);
            if (msg != null)
            {
                _context.ScheduledMessages.Remove(msg);
                await _context.SaveChangesAsync();
            }
        }

        public async Task SyncInbox()
        {
            var baseUrl = _configuration["TextBee:BaseUrl"];
            var deviceId = _configuration["TextBee:DeviceId"];
            var apiKey = _configuration["TextBee:ApiKey"];
            var url = $"{baseUrl}/gateway/devices/{deviceId}/messages";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("x-api-key", apiKey);
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return;

            List<MessageModel>? data = null;
            try
            {
                var json = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrWhiteSpace(json))
                {
                    using var doc = JsonDocument.Parse(json);
                    JsonElement root = doc.RootElement;
                    JsonElement arrayElement = root;

                    if (root.ValueKind == JsonValueKind.Object)
                    {
                        if (root.TryGetProperty("data", out var dataProp) && dataProp.ValueKind == JsonValueKind.Array)
                        {
                            arrayElement = dataProp;
                        }
                        else if (root.TryGetProperty("messages", out var messagesProp) && messagesProp.ValueKind == JsonValueKind.Array)
                        {
                            arrayElement = messagesProp;
                        }
                        else
                        {
                            return;
                        }
                    }

                    if (arrayElement.ValueKind == JsonValueKind.Array)
                    {
                        data = JsonSerializer.Deserialize<List<MessageModel>>(arrayElement.GetRawText(), new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                    }
                }
            }
            catch (JsonException)
            {
                return;
            }

            if (data == null) return;

            foreach (var m in data)
            {
                // Avoid duplicates based on timestamp and sender/recipient/body
                bool exists = await _context.Messages.AnyAsync(x =>
                    x.Timestamp == m.Timestamp &&
                    x.Sender == m.Sender &&
                    x.Recipient == m.Recipient &&
                    x.Body == m.Body &&
                    x.Direction == m.Direction);
                if (!exists)
                {
                    _context.Messages.Add(m);
                }
            }
            await _context.SaveChangesAsync();
        }
    }
}
