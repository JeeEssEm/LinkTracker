using LinkTracker.Scrapper.Domain.Tracking.Entities;

namespace LinkTracker.Scrapper.Domain.Tracking.Repositories;

public interface ISubscriptionRepository
{
    Task<bool> CreateAsync(Subscription subscription);
    Task<Subscription?> GetAsync(ChatId chatId, string url);
    Task RemoveAsync(Subscription subscription);
}
