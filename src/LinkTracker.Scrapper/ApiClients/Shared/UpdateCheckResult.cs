namespace LinkTracker.Scrapper.ApiClients.Shared;

public record UpdateCheckResult(bool HasUpdate, DateTimeOffset? NewDate, string Message = "");
