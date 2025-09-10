using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NDAProcesses.Server.Data;
using NDAProcesses.Shared.Models;
using NDAProcesses.Shared.Services;

namespace NDAProcesses.Server.Services
{
    public class MessageService : IMessageService
    {
        private readonly MessagingContext _db;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly string _baseUrl;
        private readonly string _apiKey;
        private readonly string _deviceId;

        public MessageService(MessagingContext db, IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _db = db;
            _httpClientFactory = httpClientFactory;
            _config = config;
            _baseUrl = _config["TextBee:BaseUrl"] ?? "";
            _apiKey = _config["TextBee:ApiKey"] ?? "";
            _deviceId = _config["TextBee:DeviceId"] ?? "";
        }

        public async Task SendMessage(MessageModel message)
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("x-api-key", _apiKey);
            var url = $"{_baseUrl}/gateway/devices/{_deviceId}/send-sms";
            var payload = new { recipients = new[] { message.Recipient }, message = message.Content };
            var response = await client.PostAsJsonAsync(url, payload);
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var doc = await response.Content.ReadFromJsonAsync<JsonElement>();
                    if (doc.TryGetProperty("id", out var idProp))
                    {
                        message.ExternalId = idProp.GetString();
                    }
                }
                catch { }
            }
            message.Timestamp = DateTime.UtcNow;
            message.Direction = "Sent";
            _db.Messages.Add(message);
            await _db.SaveChangesAsync();
        }

        public async Task<List<MessageModel>> GetMessages(string userName)
        {
            await SyncInboxAsync();
            return await _db.Messages
                .Where(m => m.UserName == userName || m.Direction == "Received")
                .OrderByDescending(m => m.Timestamp)
                .ToListAsync();
        }

        private async Task SyncInboxAsync()
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("x-api-key", _apiKey);
            var url = $"{_baseUrl}/gateway/devices/{_deviceId}/messages";
            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return;
            }
            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            IEnumerable<JsonElement> messages;
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Array)
            {
                messages = root.EnumerateArray();
            }
            else if (root.TryGetProperty("messages", out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                messages = arr.EnumerateArray();
            }
            else
            {
                messages = Enumerable.Empty<JsonElement>();
            }

            foreach (var m in messages)
            {
                var direction = GetString(m, "direction").ToLower();
                if (!direction.Contains("received"))
                {
                    continue;
                }
                var id = GetAnyId(m);
                if (id != null && _db.Messages.Any(x => x.ExternalId == id))
                {
                    continue;
                }
                var sender = GetString(m, "from");
                var recipient = GetString(m, "to");
                var body = GetString(m, "message");
                if (string.IsNullOrEmpty(body))
                {
                    body = GetString(m, "text");
                }
                var tsString = GetString(m, "timestamp");
                DateTime.TryParse(tsString, out var ts);
                var model = new MessageModel
                {
                    ExternalId = id,
                    Sender = sender,
                    Recipient = recipient,
                    Content = body,
                    Timestamp = ts == DateTime.MinValue ? DateTime.UtcNow : ts.ToUniversalTime(),
                    Direction = "Received"
                };
                _db.Messages.Add(model);
            }
            await _db.SaveChangesAsync();
        }

        private static string GetString(JsonElement element, string name)
        {
            return element.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String ? prop.GetString() ?? string.Empty : string.Empty;
        }

        private static string? GetAnyId(JsonElement element)
        {
            foreach (var name in new[] { "id", "message_id", "_id", "uuid" })
            {
                var val = GetString(element, name);
                if (!string.IsNullOrEmpty(val))
                {
                    return val;
                }
            }
            return null;
        }
    }
}
