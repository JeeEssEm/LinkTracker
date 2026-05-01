using LinkTracker.Scrapper.Application.QueryServices;
using LinkTracker.Scrapper.Application.Repositories;
using LinkTracker.Scrapper.Domain.Tracking.Repositories;
using LinkTracker.Scrapper.Domain.Tracking.SourceHandlers.Github;
using LinkTracker.Scrapper.Domain.Tracking.SourceHandlers.StackOverflow;
using LinkTracker.Scrapper.Infrastructure.Pipelines.BulkServices;
using LinkTracker.Scrapper.Infrastructure.Sql.QueryServices;
using LinkTracker.Scrapper.Infrastructure.Sql.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace LinkTracker.Scrapper.Infrastructure.Sql;

public static class DependencyInjectionDatabaseExtension
{
    public static IServiceCollection AddSqlRepositories(this IServiceCollection services)
    {
        services.AddScoped<IChatRepository, ChatRepository>();
        services.AddScoped<ILinkRepository, LinkRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();

        services.AddScoped<IOutboxEventRepository, SqlOutboxRepository>();

        services.AddScoped<ISubscriptionQueryService, SubscriptionQueryService>();

        services.AddScoped<IGithubRepository, GithubRepository>();
        services.AddScoped<IStackOverflowRepository, StackOverflowRepository>();

        return services;
    }
}
