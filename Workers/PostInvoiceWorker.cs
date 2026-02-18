using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PaycBillingWorker.Services;

namespace PaycBillingWorker
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

            // 1. Initialize TimeZone (Same as reference worker)
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

            // Optional: Keep your immediate run check for testing if needed
            if (_config.GetValue<bool>("SchedulerSettings:RunOnStart"))
            {
                _logger.LogInformation("RunOnStart is TRUE. Executing job immediately...");
                await RunJobAsync();
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                int targetDay;
                int targetHour;
                int targetMinute;

                // 2. Get Config (Refreshable inside loop)
                try
                {
                    targetDay = _config.GetValue<int>("SchedulerSettings:RunOnDayOfMonth");
                    targetHour = _config.GetValue<int>("SchedulerSettings:RunAtHour");
                    targetMinute = _config.GetValue<int>("SchedulerSettings:RunAtMinute");

                    // Basic validation
                    if (targetDay < 1 || targetDay > 31)
                    {
                        _logger.LogCritical("Invalid Configuration: 'RunOnDayOfMonth' must be between 1 and 31. Worker will retry in 1 hour.");
                        await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to read SchedulerSettings. Worker will retry in 1 hour.");
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                    continue;
                }

                // 3. Calculate Delay for Monthly Run
                TimeSpan delay = GetDelayUntilNextMonthlyRun(targetDay, targetHour, targetMinute);

                DateTime estimatedRunTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _namibianTimeZone).Add(delay);
                _logger.LogInformation("*****Next Invoice run scheduled for {estimatedRunTime} Namibia time. Waiting for {delay}.*****", estimatedRunTime, delay);

                // 4. Wait
                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("PostInvoiceWorker stopping during delay.");
                    break;
                }

                if (stoppingToken.IsCancellationRequested) break;

                // 5. Run The Job
                _logger.LogInformation("Running Invoice tasks at {time}", DateTimeOffset.Now);
                await RunJobAsync();

                // Small buffer to prevent tight loop if calculation is slightly off
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }

            _logger.LogInformation("PostInvoiceWorker stopped.");
        }

        private async Task RunJobAsync()
        {
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var invoiceService = scope.ServiceProvider.GetRequiredService<IInvoiceService>();
                    await invoiceService.ProcessInvoicesAsync();
                    _logger.LogInformation("*****Invoice tasks finished.*****");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during Invoice job execution.");
            }
        }

        private TimeSpan GetDelayUntilNextMonthlyRun(int targetDay, int targetHour, int targetMinute)
        {
            // Get current time in Namibia
            var nowInNamibia = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _namibianTimeZone);

            // Determine valid day for THIS month (e.g., if target is 31st but current month has 30 days)
            int daysInCurrentMonth = DateTime.DaysInMonth(nowInNamibia.Year, nowInNamibia.Month);
            int actualDayThisMonth = Math.Min(targetDay, daysInCurrentMonth);

            // Construct the run time for THIS month
            DateTime nextRunTime = new DateTime(
                nowInNamibia.Year,
                nowInNamibia.Month,
                actualDayThisMonth,
                targetHour,
                targetMinute,
                0);

            // If the time has already passed for this month, move to NEXT month
            if (nowInNamibia > nextRunTime)
            {
                var nextMonthDate = nowInNamibia.AddMonths(1);
                int daysInNextMonth = DateTime.DaysInMonth(nextMonthDate.Year, nextMonthDate.Month);
                int actualDayNextMonth = Math.Min(targetDay, daysInNextMonth);

                nextRunTime = new DateTime(
                    nextMonthDate.Year,
                    nextMonthDate.Month,
                    actualDayNextMonth,
                    targetHour,
                    targetMinute,
                    0);
            }

            return nextRunTime - nowInNamibia;
        }
    }
}