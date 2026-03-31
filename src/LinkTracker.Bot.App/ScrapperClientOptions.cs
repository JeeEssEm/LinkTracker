namespace LinkTracker.Bot.App;

public class ScrapperClientOptions
{
    public const string Position = nameof(ScrapperClientOptions);
    public string BaseUrl { get; set; }
    public bool UseGrpc { get; set; }
}