using System.Threading.RateLimiting;
using LinkTracker.Bot.App.Configurations;

namespace LinkTracker.Bot.App.Extensions;

public static class RateLimitExtensions
{
    public static IServiceCollection AddPerIpRateLimiting(this IServiceCollection services, RateLimitOptions config)
    {
        services.AddRateLimiter(options =>
        {
            options.AddPolicy(RateLimitPolicies.PerIp, context =>
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


internal static class RateLimitPolicies
{
    public const string PerIp = "per_ip";
}
