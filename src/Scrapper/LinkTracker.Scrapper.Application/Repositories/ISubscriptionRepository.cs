using LinkTracker.Scrapper.Application.ReadModels;

namespace LinkTracker.Scrapper.Application.Repositories;

public interface ISubscriptionRepository
{
    Task<bool> SubscriptionExistsAsync(long chatId, long linkId);
    Task<bool> SubscriptionExistsAsync(long chatId, string url);
    Task CreateSubscriptionAsync(long chatId, long linkId);
    Task RemoveSubscriptionAsync(long chatId, string url);
    Task<SubscribedLink?> GetSubscriptionAsync(long chatId, long linkId);
    Task<SubscribedLink?> GetSubscriptionAsync(long chatId, string url);
    Task<List<SubscribedLink>> GetSubscriptionsByChatAsync(long chatId);
}
