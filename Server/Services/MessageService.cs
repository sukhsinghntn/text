using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using NDAProcesses.Server.Data;
using NDAProcesses.Shared.Models;
using NDAProcesses.Shared.Services;
using System.Collections.Generic;
using System.Text.Json;

namespace NDAProcesses.Server.Services
{
    public class MessageService : IMessageService
    {
        private readonly MessageContext _context;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public MessageService(MessageContext context, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _context = context;
            _httpClient = httpClientFactory.CreateClient();
            _configuration = configuration;
        }

        public async Task<IEnumerable<MessageModel>> GetMessages(string userName)
        {
            await _context.Database.EnsureCreatedAsync();
            await SyncInbox();
            return await _context.Messages
                .Where(m => m.Sender == userName || m.Recipient == userName)
                .OrderByDescending(m => m.Timestamp)
                .ToListAsync();
        }

        public async Task SendMessage(MessageModel message)
        {
            await _context.Database.EnsureCreatedAsync();
            var baseUrl = _configuration["TextBee:BaseUrl"];
            var deviceId = _configuration["TextBee:DeviceId"];
            var apiKey = _configuration["TextBee:ApiKey"];
            var url = $"{baseUrl}/gateway/devices/{deviceId}/send-sms";

            var payload = new
            {
                recipients = new[] { message.Recipient },
                message = message.Body
            };

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(payload)
            };
            request.Headers.Add("x-api-key", apiKey);
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            message.Direction = "Sent";
            message.Timestamp = DateTime.UtcNow;
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();
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
