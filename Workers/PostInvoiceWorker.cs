using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PaycBillingWorker.Interfaces;
using PaycBillingWorker.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PaycBillingWorker.Workers
{
    public class PostInvoiceWorker : BackgroundService
    {
        private readonly ILogger<PostInvoiceWorker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _config;
        private readonly TimeZoneInfo _namibianTimeZone;

        public PostInvoiceWorker(
            ILogger<PostInvoiceWorker> logger,
            IServiceScopeFactory scopeFactory,
            IConfiguration config)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _config = config;

            try
            {
                _namibianTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Africa/Windhoek");
            }
            catch (TimeZoneNotFoundException)
            {
                _logger.LogCritical("Time zone 'Africa/Windhoek' not found. Defaulting to UTC.");
                _namibianTimeZone = TimeZoneInfo.Utc;
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PostInvoiceWorker Service started.");

            if (_config.GetValue<bool>("InvoiceSchedulerSettings:RunOnStart"))
            {
                _logger.LogInformation("RunOnStart TRUE. Running invoice job immediately.");
                await RunJobAsync();
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                int targetDay;
                int targetHour;
                int targetMinute;

                try
                {
                    targetDay = _config.GetValue<int>("InvoiceSchedulerSettings:RunOnDayOfMonth");
                    targetHour = _config.GetValue<int>("InvoiceSchedulerSettings:RunAtHour");
                    targetMinute = _config.GetValue<int>("InvoiceSchedulerSettings:RunAtMinute");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to read InvoiceSchedulerSettings. Retrying in 1 hour.");
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                    continue;
                }

                TimeSpan delay = GetDelayUntilNextRun(targetDay, targetHour, targetMinute);

                DateTime estimatedRunTime =
                    TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _namibianTimeZone)
                    .Add(delay);

                _logger.LogInformation(
                    "*****Next Invoice posting scheduled for {estimatedRunTime} Namibia time. Waiting {delay}.*****",
                    estimatedRunTime, delay);

                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("PostInvoiceWorker stopping during delay.");
                    break;
                }

                if (stoppingToken.IsCancellationRequested)
                    break;

                _logger.LogInformation("Running invoice posting at {time}", DateTimeOffset.Now);

                await RunJobAsync();

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }

            _logger.LogInformation("PostInvoiceWorker stopped.");
        }

        private async Task RunJobAsync()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var invoiceService = scope.ServiceProvider.GetRequiredService<IInvoiceService>();

                await invoiceService.ProcessInvoicesAsync();

                _logger.LogInformation("*****Invoice posting finished successfully.*****");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during invoice posting job execution.");
            }
        }

        private TimeSpan GetDelayUntilNextRun(int targetDay, int targetHour, int targetMinute)
        {
            var nowInNamibia = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _namibianTimeZone);

            DateTime nextRun = new DateTime(
                nowInNamibia.Year,
                nowInNamibia.Month,
                targetDay,
                targetHour,
                targetMinute,
                0);

            if (nowInNamibia > nextRun)
                nextRun = nextRun.AddMonths(1);

            return nextRun - nowInNamibia;
        }
    }
}