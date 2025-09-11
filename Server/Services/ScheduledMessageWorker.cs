using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NDAProcesses.Server.Data;
using NDAProcesses.Shared.Models;
using NDAProcesses.Shared.Services;
using System;

namespace NDAProcesses.Server.Services
{
    public class ScheduledMessageWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ScheduledMessageWorker> _logger;

        public ScheduledMessageWorker(IServiceScopeFactory scopeFactory, ILogger<ScheduledMessageWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<MessageContext>();
                    var service = scope.ServiceProvider.GetRequiredService<IMessageService>();

                    var due = await context.ScheduledMessages
                        .Where(m => !m.Sent && m.ScheduledFor <= DateTime.UtcNow)
                        .ToListAsync(stoppingToken);

                    foreach (var msg in due)
                    {
                        await service.SendMessage(new MessageModel
                        {
                            Sender = msg.Sender,
                            SenderName = msg.SenderName,
                            SenderDepartment = msg.SenderDepartment,
                            Recipient = msg.Recipient,
                            Body = msg.Body
                        });
                        msg.Sent = true;
                    }

                    if (due.Count > 0)
                    {
                        await context.SaveChangesAsync(stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing scheduled messages");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
