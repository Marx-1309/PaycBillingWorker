namespace PaycBillingWorker.Models
{
    public class SchedulerSettings
    {
        public int RunOnDayOfMonth { get; set; }
        public int RunAtHour { get; set; }
        public int RunAtMinute { get; set; }
        public bool RunOnStart { get; set; }
    }
}
