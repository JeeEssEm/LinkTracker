namespace LinkTracker.Scrapper.Configurations;

public class MetricsConfiguration
{
    public const string Position = nameof(MetricsConfiguration);
    public int CheckNumberOfLinksSeconds { get; set; } = 15;
}