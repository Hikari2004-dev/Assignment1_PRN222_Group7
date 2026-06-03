using Assignment1_PRN222_Group7_BLL.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7.BackgroundServices
{
    public class SubscriptionExpirationWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SubscriptionExpirationWorker> _logger;

        public SubscriptionExpirationWorker(
            IServiceProvider serviceProvider,
            ILogger<SubscriptionExpirationWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Subscription Expiration Worker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Checking for expired subscriptions at: {Time}", DateTimeOffset.Now);

                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var subscriptionService = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();
                        await subscriptionService.ProcessExpiredSubscriptionsAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while processing expired subscriptions.");
                }

                // Chạy định kỳ mỗi 1 phút để kiểm tra
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }

            _logger.LogInformation("Subscription Expiration Worker stopped.");
        }
    }
}
