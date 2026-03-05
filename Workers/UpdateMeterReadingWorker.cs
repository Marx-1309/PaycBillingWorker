using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PaycBillingWorker.Interfaces;
using PaycBillingWorker.Repositories;

namespace PaycBillingWorker.Workers
{
    public class UpdateMeterReadingWorker : BackgroundService
    {
        private readonly ILogger<UpdateMeterReadingWorker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _config;
        private readonly TimeZoneInfo _namibianTimeZone;

        public UpdateMeterReadingWorker(
            ILogger<UpdateMeterReadingWorker> logger,
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
            _logger.LogInformation("UpdateMeterReadingWorker Service started.");

            if (_config.GetValue<bool>("MeterSchedulerSettings:RunOnStart"))
            {
                _logger.LogInformation("RunOnStart TRUE. Running meter sync immediately.");
                await RunJobAsync();
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                int targetDay;
                int targetHour;
                int targetMinute;

                try
                {
                    targetDay = _config.GetValue<int>("MeterSchedulerSettings:RunOnDayOfMonth");
                    targetHour = _config.GetValue<int>("MeterSchedulerSettings:RunAtHour");
                    targetMinute = _config.GetValue<int>("MeterSchedulerSettings:RunAtMinute");

                    if (targetDay < 1 || targetDay > 31)
                    {
                        _logger.LogCritical("Invalid MeterSchedulerSettings: RunOnDayOfMonth must be between 1 and 31.");
                        await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to read MeterSchedulerSettings. Retrying in 1 hour.");
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                    continue;
                }

                TimeSpan delay = GetDelayUntilNextMonthlyRun(targetDay, targetHour, targetMinute);

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

                if (stoppingToken.IsCancellationRequested)
                    break;

                _logger.LogInformation("Running meter reading sync at {time}", DateTimeOffset.Now);

                await RunJobAsync();

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
                            var reading = Convert.ToInt32(
                                response.Result.MeterReading.JsonMetadata.Reading);

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
                int actualDayNextMonth = Math.Min(targetDay, daysInNextMonth);

                nextRunTime = new DateTime(
                    nextMonth.Year,
                    nextMonth.Month,
                    actualDayNextMonth,
                    targetHour,
                    targetMinute,
                    0);
            }

            return nextRunTime - nowInNamibia;
        }
    }
}