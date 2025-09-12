using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NDAProcesses.Shared.Services;
using System;

namespace NDAProcesses.Server.Services
{
    public class InboxPollerWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<InboxPollerWorker> _logger;

        public InboxPollerWorker(IServiceScopeFactory scopeFactory, ILogger<InboxPollerWorker> logger)
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
                    var service = scope.ServiceProvider.GetRequiredService<IMessageService>();
                    await service.SyncInbox();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error syncing inbox");
                }

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}
