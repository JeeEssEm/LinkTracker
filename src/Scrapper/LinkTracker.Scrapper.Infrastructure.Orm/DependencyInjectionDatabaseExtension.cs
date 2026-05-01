using LinkTracker.Scrapper.Application.Abstractions.UnitOfWork;
using LinkTracker.Scrapper.Application.QueryServices;
using LinkTracker.Scrapper.Application.Repositories;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Store;
using LinkTracker.Scrapper.Domain.Tracking.Repositories;
using LinkTracker.Scrapper.Domain.Tracking.SourceHandlers.Github;
using LinkTracker.Scrapper.Domain.Tracking.SourceHandlers.StackOverflow;
using LinkTracker.Scrapper.Infrastructure.Orm.Context;
using LinkTracker.Scrapper.Infrastructure.Orm.QueryServices;
using LinkTracker.Scrapper.Infrastructure.Orm.Repositories;
using LinkTracker.Scrapper.Infrastructure.Pipelines.BulkServices;
using LinkTracker.Scrapper.Infrastructure.Pipelines.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LinkTracker.Scrapper.Infrastructure.Orm;

public static class DependencyInjectionDatabaseExtension
{
    public static IServiceCollection AddOrmContext(this IServiceCollection services)
    {
        services.AddDbContext<LinkTrackerContext>((sp, options) =>
        {
            var session = sp.GetRequiredService<DatabaseSession>();
            var connection = session.GetOrCreateConnection();

            options.UseNpgsql(connection).UseSnakeCaseNamingConvention();
        });
        services.AddScoped<ITransactionParticipant>(sp =>
        {
            var dbContext = sp.GetRequiredService<LinkTrackerContext>();
            var p = new TransactionParticipant(dbContext);

            return p;
        });

        return services;
    }

    public static IServiceCollection AddOrmRepositories(this IServiceCollection services)
    {
        services.AddScoped<ILinkRepository, LinkRepository>();
        services.AddScoped<IChatRepository, ChatRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();

        services.AddScoped<ISubscriptionQueryService, SubscriptionQueryService>();

        services.AddScoped<IOutboxEventRepository, OrmOutboxRepository>();

        services.AddScoped<IGithubRepository, OrmGithubRepository>();
        services.AddScoped<IStackOverflowRepository, StackOverflowRepository>();

        return services;
    }
}
