namespace LinkTracker.Bot.App.Configurations;


public class BotOptions
{
    public const string Position = nameof(BotOptions);
    public required string Token { get; init; }
}
