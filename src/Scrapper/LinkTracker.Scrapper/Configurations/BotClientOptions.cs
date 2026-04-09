namespace LinkTracker.Scrapper.Configurations;

public class BotClientOptions
{
    public const string Position = nameof(BotClientOptions);
    public required string BaseUrl { get; set; }
    public bool UseGrpc { get; set; }
}
