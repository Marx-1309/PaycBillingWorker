using System;
using System.Collections.Concurrent;
using System.Threading;

namespace PaycBillingWorker.Services
{
    public interface IWorkerHealthService
    {
        void UpdateWorkerLastRun(string workerName);
        DateTime? GetLastRun(string workerName);
    }

    public class WorkerHealthService : IWorkerHealthService
    {
        private readonly ConcurrentDictionary<string, DateTime> _workerLastRun = new();

        public void UpdateWorkerLastRun(string workerName)
        {
            _workerLastRun[workerName] = DateTime.UtcNow;
        }

        public DateTime? GetLastRun(string workerName)
        {
            return _workerLastRun.TryGetValue(workerName, out var dt) ? dt : null;
        }
    }
}
