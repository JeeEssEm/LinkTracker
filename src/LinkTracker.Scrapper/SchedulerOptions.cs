namespace LinkTracker.Scrapper;

public class SchedulerOptions
{
    public const string Position = nameof(SchedulerOptions);
    public int CheckIntervalInMinutes { get; set; } = 5;
}
