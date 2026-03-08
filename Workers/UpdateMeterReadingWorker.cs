using Microsoft.Extensions.Options; // Required for IOptionsMonitor
using PaycBillingWorker.Interfaces;
using PaycBillingWorker.Models;
using PaycBillingWorker.Repositories;
using PaycBillingWorker.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PaycBillingWorker.Workers
{
    public class UpdateMeterReadingWorker : BackgroundService
    {
        private readonly ILogger<UpdateMeterReadingWorker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IOptionsMonitor<SchedulerSettings> _options; // Changed
        private readonly TimeZoneInfo _namibianTimeZone;
        private readonly IWorkerHealthService _workerHealthService;

        public UpdateMeterReadingWorker(
            ILogger<UpdateMeterReadingWorker> logger,
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
            _logger.LogInformation("UpdateMeterReadingWorker Service started.");

            // Initial startup delay
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            // Fetch current settings for the start check
            var currentSettings = _options.Get("MeterSchedulerSettings");

            if (currentSettings.RunOnStart)
            {
                _logger.LogInformation("RunOnStart TRUE. Running meter sync immediately.");
                await RunJobAsync();
                _workerHealthService.UpdateWorkerLastRun(nameof(UpdateMeterReadingWorker));
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                // Pull FRESH settings from appsettings.json
                var settings = _options.Get("MeterSchedulerSettings");

                TimeSpan delay = GetDelayUntilNextMonthlyRun(
                    settings.RunOnDayOfMonth,
                    settings.RunAtHour,
                    settings.RunAtMinute);

                DateTime estimatedRunTime = TimeZoneInfo
                    .ConvertTimeFromUtc(DateTime.UtcNow, _namibianTimeZone)
                    .Add(delay);

                _logger.LogInformation(
                    "*****Next Meter Reading sync scheduled for {estimatedRunTime} Namibia time. Waiting {delay}.*****",
                    estimatedRunTime, delay);

                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("UpdateMeterReadingWorker stopping during delay.");
                    break;
                }

                if (stoppingToken.IsCancellationRequested) break;

                _logger.LogInformation("Running meter reading sync at {time}", DateTimeOffset.Now);

                await RunJobAsync();

                // Update health service specifically for THIS worker
                _workerHealthService.UpdateWorkerLastRun(nameof(UpdateMeterReadingWorker));

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }

            _logger.LogInformation("UpdateMeterReadingWorker stopped.");
        }

        private async Task RunJobAsync()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var repository = scope.ServiceProvider.GetRequiredService<MeterReadingRepository>();
                var meterService = scope.ServiceProvider.GetRequiredService<IMeterReadingService>();

                var serialNumbers = await repository.GetCustomerSerialNumbersAsync();

                foreach (var serial in serialNumbers)
                {
                    try
                    {
                        var response = await meterService.GetReadingsBySerialAsync(serial, 1, 1);

                        if (response?.Result?.MeterReading?.JsonMetadata != null)
                        {
                            var reading = Convert.ToInt32(response.Result.MeterReading.JsonMetadata.Reading);
                            await repository.UpdateCustomerReadingAsync(reading, serial.Trim());
                            _logger.LogInformation("Updated meter {serial} with reading {reading}", serial, reading);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed updating meter {serial}", serial);
                    }
                }

                _logger.LogInformation("*****Meter reading sync finished.*****");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during meter reading job execution.");
            }
        }

        private TimeSpan GetDelayUntilNextMonthlyRun(int targetDay, int targetHour, int targetMinute)
        {
            var nowInNamibia = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _namibianTimeZone);

            int daysInCurrentMonth = DateTime.DaysInMonth(nowInNamibia.Year, nowInNamibia.Month);
            int actualDayThisMonth = Math.Min(targetDay, daysInCurrentMonth);

            DateTime nextRunTime = new DateTime(
                nowInNamibia.Year,
                nowInNamibia.Month,
                actualDayThisMonth,
                targetHour,
                targetMinute,
                0);

            if (nowInNamibia > nextRunTime)
            {
                var nextMonth = nowInNamibia.AddMonths(1);
                int daysInNextMonth = DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month);
                nextRunTime = new DateTime(
                    nextMonth.Year,
                    nextMonth.Month,
                    Math.Min(targetDay, daysInNextMonth),
                    targetHour,
                    targetMinute,
                    0);
            }

            return nextRunTime - nowInNamibia;
        }
    }
}