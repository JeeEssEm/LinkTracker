using LinkTracker.Scrapper.Application.Abstractions;
using LinkTracker.Scrapper.Application.Repositories;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Store;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Store;
using LinkTracker.Scrapper.Infrastructure.Orm.Context;
using LinkTracker.Scrapper.Infrastructure.Orm.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LinkTracker.Scrapper.Infrastructure.Orm;

public static class DependencyInjectionDatabaseExtension
{
    public static IServiceCollection AddOrmDatabase(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<LinkTrackerContext>(options =>
        {
            options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();
        });
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }

    public static IServiceCollection AddOrmRepositories(this IServiceCollection services)
    {
        services.AddScoped<ILinkRepository, LinkRepository>();
        services.AddScoped<IChatRepository, ChatRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<IStackOverflowStore, OrmStackOverflowStore>();
        services.AddScoped<IGithubStore, OrmGithubStore>();

        return services;
    }
}
