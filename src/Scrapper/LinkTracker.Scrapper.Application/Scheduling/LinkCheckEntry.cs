namespace LinkTracker.Scrapper.Application.Scheduling;

public record LinkCheckEntry(long Id, string Url, string CheckerKey, DateTime? LastCheckAt, IReadOnlyList<long> ChatIdsToNotify);
