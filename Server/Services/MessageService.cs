using System;
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
        private readonly IUserService _userService;

        public MessageService(MessageContext context, IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<MessageService> logger, IUserService userService)
        {
            _context = context;
            _httpClient = httpClientFactory.CreateClient();
            _configuration = configuration;
            _logger = logger;
            _userService = userService;
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
            var user = await _userService.GetUserData(message.Sender);
            message.SenderName = user?.DisplayName ?? string.Empty;
            message.SenderDepartment = user?.Department ?? string.Empty;

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

        public async Task<IEnumerable<ContactModel>> GetContacts()
        {
            await _context.Database.EnsureCreatedAsync();
            return await _context.Contacts
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

        public async Task DeleteContact(int id)
        {
            await _context.Database.EnsureCreatedAsync();
            var contact = await _context.Contacts.FirstOrDefaultAsync(c => c.Id == id);
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
            await _context.Database.EnsureCreatedAsync();

            var baseUrl = _configuration["TextBee:BaseUrl"];
            var deviceId = _configuration["TextBee:DeviceId"];
            var apiKey = _configuration["TextBee:ApiKey"];

            var receivedUrl = $"{baseUrl}/gateway/devices/{deviceId}/get-received-sms";
            var allUrl = $"{baseUrl}/gateway/devices/{deviceId}/messages";

            var messages = await FetchMessages(receivedUrl, apiKey);
            if (!messages.Any())
            {
                var all = await FetchMessages(allUrl, apiKey);
                messages = all.Where(IsReceived);
            }

            var saved = 0;

            foreach (var item in messages)
            {
                var msgId = GuessId(item);
                if (!string.IsNullOrEmpty(msgId) && await _context.Messages.AnyAsync(m => m.ExternalId == msgId))
                    continue;

                var sender = GetString(item, "from", "sender");
                var recipient = GetString(item, "to", "recipient");
                if (string.IsNullOrWhiteSpace(sender) || string.IsNullOrWhiteSpace(recipient))
                    continue;

                var body = GetString(item, "message", "text", "body");
                var tsString = GetString(item, "timestamp", "created_at");
                var timestamp = DateTime.UtcNow;
                if (!string.IsNullOrWhiteSpace(tsString))
                    DateTime.TryParse(tsString, out timestamp);

                // Map to the internal user who last sent to this number
                var lastSent = await _context.Messages
                    .Where(x => x.Recipient == sender && x.Direction == "Sent")
                    .OrderByDescending(x => x.Timestamp)
                    .FirstOrDefaultAsync();
                if (lastSent != null)
                {
                    recipient = lastSent.Sender;
                }

                var message = new MessageModel
                {
                    Sender = sender,
                    Recipient = recipient,
                    Body = body,
                    Direction = "Received",
                    Timestamp = timestamp,
                    ExternalId = msgId ?? string.Empty
                };

                _context.Messages.Add(message);
                saved++;
            }

            if (saved > 0)
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Stored {Count} new incoming messages", saved);
            }
        }

        private async Task<IEnumerable<JsonElement>> FetchMessages(string url, string apiKey)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("x-api-key", apiKey);
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return Enumerable.Empty<JsonElement>();
                var json = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(json))
                    return Enumerable.Empty<JsonElement>();
                using var doc = JsonDocument.Parse(json);
                return NormalizeMessages(doc.RootElement)
                    .Select(e => e.Clone())
                    .ToList();
            }
            catch
            {
                return Enumerable.Empty<JsonElement>();
            }
        }

        private static string? GuessId(JsonElement m)
        {
            foreach (var name in new[] { "id", "message_id", "_id", "uuid" })
            {
                if (m.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String)
                {
                    var v = prop.GetString();
                    if (!string.IsNullOrWhiteSpace(v))
                        return v;
                }
            }
            return null;
        }

        private static string GetString(JsonElement m, params string[] names)
        {
            foreach (var n in names)
            {
                if (m.TryGetProperty(n, out var prop) && prop.ValueKind == JsonValueKind.String)
                    return prop.GetString() ?? string.Empty;
            }
            return string.Empty;
        }

        private static bool IsReceived(JsonElement m)
        {
            var dir = GetString(m, "direction", "type").ToLowerInvariant();
            return dir.Contains("recv") || dir == "inbound" || dir == "received";
        }

        private static IEnumerable<JsonElement> NormalizeMessages(JsonElement root)
        {
            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in root.EnumerateArray())
                    yield return item;
                yield break;
            }

            if (root.ValueKind == JsonValueKind.Object)
            {
                foreach (var key in new[] { "messages", "data", "results", "items" })
                {
                    if (root.TryGetProperty(key, out var arr) && arr.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in arr.EnumerateArray())
                            yield return item;
                        yield break;
                    }
                }

                if (root.TryGetProperty("from", out _) || root.TryGetProperty("to", out _) ||
                    root.TryGetProperty("message", out _) || root.TryGetProperty("text", out _) ||
                    root.TryGetProperty("body", out _) || root.TryGetProperty("timestamp", out _) ||
                    root.TryGetProperty("created_at", out _))
                {
                    yield return root;
                }
            }
        }
    }
}
