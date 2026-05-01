namespace LinkTracker.Scrapper.Application.ReadModels;

public record SubscriptionDto(Guid Id, string Url, IReadOnlyList<string> Tags);
// TODO: это точно не тут должно быть
