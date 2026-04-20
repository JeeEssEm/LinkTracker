namespace LinkTracker.Scrapper.Application.Scraping.Pipelines.Shared.Data;

public record LinkMetadata(long Id, string Url, IReadOnlyList<long> ChatIdsToNotify, bool IsFirstCheck = false);
