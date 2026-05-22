using System.Threading.RateLimiting;
using LinkTracker.Scrapper.Configurations;

namespace LinkTracker.Scrapper.Extensions;

public static class RateLimitExtensions
{
    public static IServiceCollection AddPerIpRateLimiting(this IServiceCollection services, RateLimitOptions config)
    {
        services.AddRateLimiter(options =>
        {
            options.AddPolicy(RateLimitPolitics.PerIpLimit, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = config.PermitLimit,
                        Window = TimeSpan.FromSeconds(config.WindowSeconds),
                        QueueLimit = config.QueueLimit
                    }));

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });

        return services;
    }
}

public static class RateLimitPolitics
{
    public const string PerIpLimit = "per_ip";
}
