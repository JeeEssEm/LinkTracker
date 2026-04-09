namespace LinkTracker.Scrapper.Configurations;

public class SchedulerOptions
{
    public const string Position = nameof(SchedulerOptions);
    public int CheckIntervalInMinutes { get; set; } = 5;
    public int MaxDegreeOfParallelism { get; set; } = 10;
    public int BatchSize { get; set; } = 100;
}
