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
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _config;

        public PostInvoiceWorker(ILogger<PostInvoiceWorker> logger, IServiceProvider serviceProvider, IConfiguration config)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _config = config;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PaycBillingWorker Service started.");

            // ---------------------------------------------------------
            // 1. IMMEDIATE RUN CHECK (For Testing)
            // ---------------------------------------------------------
            if (_config.GetValue<bool>("SchedulerSettings:RunOnStart"))
            {
                _logger.LogInformation("RunOnStart is TRUE. Executing job immediately...");
                await RunJobAsync();
            }

            // ---------------------------------------------------------
            // 2. SCHEDULING LOOP
            // ---------------------------------------------------------
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Get schedule settings
                    int targetDay = _config.GetValue<int>("SchedulerSettings:RunOnDayOfMonth");
                    int targetHour = _config.GetValue<int>("SchedulerSettings:RunAtHour");
                    int targetMinute = _config.GetValue<int>("SchedulerSettings:RunAtMinute");

                    DateTime now = DateTime.Now;
                    DateTime nextRun = new DateTime(now.Year, now.Month, targetDay, targetHour, targetMinute, 0);

                    // If we passed the run time for this month, schedule for next month
                    if (now >= nextRun)
                    {
                        nextRun = nextRun.AddMonths(1);
                    }

                    TimeSpan delay = nextRun - now;
                    _logger.LogInformation($"Next scheduled run: {nextRun} (Delay: {delay})");

                    // Wait until the scheduled time
                    await Task.Delay(delay, stoppingToken);

                    // Execute if not cancelled
                    if (!stoppingToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("Executing scheduled invoice job...");
                        await RunJobAsync();
                    }
                }
                catch (TaskCanceledException)
                {
                    // Ignore on shutdown
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred in the scheduler loop.");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }
        }

        // Helper method to encapsulate the job execution logic
        private async Task RunJobAsync()
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var invoiceService = scope.ServiceProvider.GetRequiredService<IInvoiceService>();
                    await invoiceService.ProcessInvoicesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during job execution.");
            }
        }
    }
}