using Microsoft.Extensions.Options; // Required for IOptionsMonitor
using PaycBillingWorker.Interfaces;
using PaycBillingWorker.Models;
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
        private readonly IOptionsMonitor<SchedulerSettings> _options; // Changed
        private readonly TimeZoneInfo _namibianTimeZone;
        private readonly IWorkerHealthService _workerHealthService;

        public PostInvoiceWorker(
            ILogger<PostInvoiceWorker> logger,
            IServiceScopeFactory scopeFactory,
            IOptionsMonitor<SchedulerSettings> options, // Injected
            IWorkerHealthService workerHealthService)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _options = options;
            _workerHealthService = workerHealthService;

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

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            // Access latest settings for the 'RunOnStart' check
            var currentSettings = _options.Get("InvoiceSchedulerSettings");

            if (currentSettings.RunOnStart)
            {
                _logger.LogInformation("RunOnStart TRUE. Running invoice job immediately.");
                await RunJobAsync();
                _workerHealthService.UpdateWorkerLastRun(nameof(PostInvoiceWorker));
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                // Fetch FRESH settings from the file at the start of every loop iteration
                var settings = _options.Get("InvoiceSchedulerSettings");

                TimeSpan delay = GetDelayUntilNextRun(
                    settings.RunOnDayOfMonth,
                    settings.RunAtHour,
                    settings.RunAtMinute);

                DateTime estimatedRunTime = TimeZoneInfo
                    .ConvertTimeFromUtc(DateTime.UtcNow, _namibianTimeZone)
                    .Add(delay);

                _logger.LogInformation(
                    "*****Next Invoice posting scheduled for {estimatedRunTime} Namibia time. Waiting {delay}.*****",
                    estimatedRunTime, delay);

                try
                {
                    // If you change appsettings during this delay, the worker will 
                    // pick up the NEW values AFTER this current delay finishes.
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                if (stoppingToken.IsCancellationRequested) break;

                _logger.LogInformation("Running invoice posting at {time}", DateTimeOffset.Now);

                await RunJobAsync();
                _workerHealthService.UpdateWorkerLastRun(nameof(PostInvoiceWorker));

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
            _logger.LogInformation("******************Skip To Run Job For Now******************");
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

            // Handle months where targetDay doesn't exist (e.g. Feb 30)
            int daysInCurrentMonth = DateTime.DaysInMonth(nowInNamibia.Year, nowInNamibia.Month);
            int actualDay = Math.Min(targetDay, daysInCurrentMonth);

            DateTime nextRun = new DateTime(nowInNamibia.Year, nowInNamibia.Month, actualDay, targetHour, targetMinute, 0);

            if (nowInNamibia > nextRun)
            {
                var nextMonth = nowInNamibia.AddMonths(1);
                int daysInNextMonth = DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month);
                nextRun = new DateTime(nextMonth.Year, nextMonth.Month, Math.Min(targetDay, daysInNextMonth), targetHour, targetMinute, 0);
            }

            return nextRun - nowInNamibia;
        }
    }
}