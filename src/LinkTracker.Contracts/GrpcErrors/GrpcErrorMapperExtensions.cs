using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace LinkTracker.Contracts.GrpcErrors;

public static class GrpcErrorMapperExtensions
{
    public static IServiceCollection AddGrpcErrorMapping(this IServiceCollection services, params Assembly[] assemblies)
    {
        var profileTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(GrpcErrorProfile)));

        foreach (var type in profileTypes)
        {
            services.AddSingleton(typeof(GrpcErrorProfile), type);
        }

        services.AddSingleton<GrpcErrorMapper>();

        return services;
    }
}