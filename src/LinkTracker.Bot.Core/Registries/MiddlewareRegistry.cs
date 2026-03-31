using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using LinkTracker.Bot.Core.Dispatchers;
using LinkTracker.Bot.Core.Middlewares;
using LinkTracker.Bot.Core.StateMachine;

namespace LinkTracker.Bot.Core.Registries;

public class MiddlewareRegistry
{
    private Func<UpdateContext, IServiceProvider, Task>? _terminalFactory;
    internal List<Func<IServiceProvider, UpdateDelegate, InternalUpdateDelegate>> Middlewares { get; } = new();
    private IServiceCollection _services;

    public MiddlewareRegistry(IServiceCollection sc)
    {
        _services = sc;
    }

    public Task TerminalHandler(UpdateContext ctx)
    {
        if (_terminalFactory == null)
        {
            throw new InvalidOperationException("Terminal handler (dispatcher) is not configured.");
        }

        return _terminalFactory(ctx, ctx.Services);
    }

    public void RegisterMiddleware<TMiddleware>() where TMiddleware : class
    {
        _services.AddSingleton<TMiddleware>();

        Func<IServiceProvider, UpdateDelegate, InternalUpdateDelegate> invoker = (sp, next) =>
        {
            var type = typeof(TMiddleware);
            var method = type.GetMethod("InvokeAsync");

            if (method == null)
            {
                throw new ArgumentException($"Middleware with type {typeof(TMiddleware)} does not has method InvokeAsync");
            }

            var methodParams = method.GetParameters();

            var instance = ActivatorUtilities.CreateInstance(sp, type, next);

            var contextParam = Expression.Parameter(typeof(UpdateContext), "ctx");
            var servicesParam = Expression.Parameter(typeof(IServiceProvider), "sp");

            var args = new Expression[methodParams.Length];
            args[0] = contextParam;

            for (var i = 1; i < methodParams.Length; i++)
            {
                var paramType = methodParams[i].ParameterType;
                var getServiceCall = Expression.Call(
                    null,
                    typeof(ServiceProviderServiceExtensions).GetMethod("GetRequiredService", [typeof(IServiceProvider), typeof(Type)])!,
                    servicesParam,
                    Expression.Constant(paramType)
                );

                args[i] = Expression.Convert(getServiceCall, paramType);
            }

            var instanceConst = Expression.Constant(instance);
            var methodCall = Expression.Call(instanceConst, method, args);

            return Expression.Lambda<InternalUpdateDelegate>(methodCall, contextParam, servicesParam).Compile();
        };
        Middlewares.Add(invoker);
    }

    public void UseDefaultDispatcher()
    {
        _terminalFactory = (ctx, sp) =>
        {
            var dispatcher = sp.GetRequiredService<ActionDispatcher>();
            return dispatcher.DispatchAsync(ctx.Update, ctx.CancellationToken);
        };
    }
}