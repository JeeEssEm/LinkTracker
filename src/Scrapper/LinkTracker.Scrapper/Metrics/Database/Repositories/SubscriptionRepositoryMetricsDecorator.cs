using System.Diagnostics;
using LinkTracker.Scrapper.Domain.Tracking.Entities;
using LinkTracker.Scrapper.Domain.Tracking.Repositories;

namespace LinkTracker.Scrapper.Metrics.Database.Repositories;

public class SubscriptionRepositoryMetricsDecorator(ISubscriptionRepository subscriptionRepo, ApplicationMetrics metrics) : ISubscriptionRepository
{
    public async Task<bool> CreateAsync(Subscription subscription)
    {
        var sw = Stopwatch.StartNew();
        var result = await subscriptionRepo.CreateAsync(subscription);
        metrics.RequestDuration.Record(
            sw.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("scope", RequestDurationScopes.Database),
            new KeyValuePair<string, object?>("scope_type", "subscriptions")
        );
        return result;
    }

    public async Task<Subscription?> GetAsync(ChatId chatId, string url)
    {
        var sw = Stopwatch.StartNew();
        var result = await subscriptionRepo.GetAsync(chatId, url);
        metrics.RequestDuration.Record(
            sw.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("scope", RequestDurationScopes.Database),
            new KeyValuePair<string, object?>("scope_type", "subscriptions")
        );
        return result;
    }

    public async Task RemoveAsync(Subscription subscription)
    {
        var sw = Stopwatch.StartNew();
        await subscriptionRepo.RemoveAsync(subscription);
        metrics.RequestDuration.Record(
            sw.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("scope", RequestDurationScopes.Database),
            new KeyValuePair<string, object?>("scope_type", "subscriptions")
        );
    }
}
