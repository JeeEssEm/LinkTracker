namespace LinkTracker.Scrapper.Application.Scheduling;

public record NotifyEntry(long LinkId, string Url, string Message, IReadOnlyList<long> ChatIds);
