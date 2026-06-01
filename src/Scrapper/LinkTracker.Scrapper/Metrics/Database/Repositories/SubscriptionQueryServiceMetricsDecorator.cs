using System.Diagnostics;
using LinkTracker.Scrapper.Application.QueryServices;
using LinkTracker.Scrapper.Contracts.Dtos.Links;
using LinkTracker.Scrapper.Domain.Tracking.Entities;

namespace LinkTracker.Scrapper.Metrics.Database.Repositories;

public class SubscriptionQueryServiceMetricsDecorator(ISubscriptionQueryService subscriptionQueryService, ApplicationMetrics metrics) : ISubscriptionQueryService
{
    public async Task<IReadOnlyList<LinkResponse>> ListSubscriptionsByChatAsync(ChatId chatId)
    {
        var sw = Stopwatch.StartNew();
        var result = await subscriptionQueryService.ListSubscriptionsByChatAsync(chatId);
        metrics.RequestDuration.Record(
            sw.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("scope", RequestDurationScopes.Database),
            new KeyValuePair<string, object?>("scope_type", "subscriptions")
        );
        return result;
    }
}
