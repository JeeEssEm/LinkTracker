namespace LinkTracker.Bot.App.Metrics;

public class CommandDurationLabels
{
    public const string Scope = "scope";
    public const string ScopeType = "scope_type";
}

public class CommandDurationScopes
{
    public const string ScrapperSyncApi = "scrapper_sync_api";
    public const string ScrapperAsyncApi = "scrapper_async_api";
}
