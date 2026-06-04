using LinkTracker.Scrapper.Application.QueryServices;
using LinkTracker.Scrapper.Application.Repositories;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Store;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Store;
using LinkTracker.Scrapper.Domain.Tracking.Repositories;
using LinkTracker.Scrapper.Domain.Tracking.SourceHandlers.Github;
using LinkTracker.Scrapper.Domain.Tracking.SourceHandlers.StackOverflow;
using LinkTracker.Scrapper.Metrics.Database.Repositories;

namespace LinkTracker.Scrapper.Metrics.Database;

public static class MetricsDecoratorExtensions
{
    public static IServiceCollection DecorateRepositoriesWithMetrics(this IServiceCollection services)
    {
        services.Decorate<IChatRepository, ChatRepositoryMetricsDecorator>();

        services.Decorate<ILinkRepository, LinkRepositoryMetricsDecorator>();
        services.Decorate<ILinkBulkRepository, LinkBulkRepositoryMetricsDecorator>();

        services.Decorate<ISubscriptionRepository, SubscriptionRepositoryMetricsDecorator>();
        services.Decorate<ISubscriptionQueryService, SubscriptionQueryServiceMetricsDecorator>();

        services.Decorate<IOutboxEventRepository, OutboxEventRepositoryMetricsDecorator>();

        services.Decorate<IGithubStore, GithubStoreMetricsDecorator>();
        services.Decorate<IGithubRepository, GithubRepositoryMetricsDecorator>();

        services.Decorate<IStackOverflowStore, StackOverflowStoreMetricsDecorator>();
        services.Decorate<IStackOverflowRepository, StackOverflowRepositoryMetricsDecorator>();

        return services;
    }
}