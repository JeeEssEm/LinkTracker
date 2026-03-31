namespace LinkTracker.Scrapper;

public class BotClientOptions
{
    public const string Position = nameof(BotClientOptions);
    public string BaseUrl { get; set; }
    public bool UseGrpc { get; set; }
}