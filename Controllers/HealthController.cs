using Microsoft.AspNetCore.Mvc;
using PaycBillingWorker.Services;
using System;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly IWorkerHealthService _workerHealthService;

    public HealthController(IWorkerHealthService workerHealthService)
    {
        _workerHealthService = workerHealthService;
    }

    [HttpGet]
    public IActionResult Get()
    {
        var workers = new[]
        {
            "UpdateMeterReadingWorker",
            "PostInvoiceWorker"
        };

        var workerStatus = new System.Collections.Generic.Dictionary<string, string>();

        foreach (var worker in workers)
        {
            var lastRun = _workerHealthService.GetLastRun(worker);
            if (lastRun.HasValue)
            {
                var age = DateTime.UtcNow - lastRun.Value;
                workerStatus[worker] = $"Last run {age.TotalMinutes:F1} minutes ago";
            }
            else
            {
                workerStatus[worker] = "Has not run yet";
            }
        }

        return Ok(new
        {
            Api = "Healthy",
            Workers = workerStatus,
            ServerTime = DateTime.UtcNow
        });
    }
}