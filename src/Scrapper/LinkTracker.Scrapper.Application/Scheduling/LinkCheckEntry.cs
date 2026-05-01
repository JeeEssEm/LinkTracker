using LinkTracker.Scrapper.Domain.Tracking.ValueObjects;

namespace LinkTracker.Scrapper.Application.Scheduling;

public record LinkCheckEntry(Guid Id, string Url, LinkSourceType SourceType, DateTimeOffset? LastCheckAt, IReadOnlyList<long> ChatIdsToNotify);
