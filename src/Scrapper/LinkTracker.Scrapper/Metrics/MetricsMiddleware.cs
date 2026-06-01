namespace LinkTracker.Scrapper.Metrics;

public class MetricsMiddleware(RequestDelegate next, ApplicationMetrics metrics)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // В ТЗ ничего не сказано о том, что подразумевается под source...
        var source = context.Request.Headers["X-Source"].FirstOrDefault() ?? context.Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        metrics.ApiRequests.Add(1, new KeyValuePair<string, object?>("source", source));
        await next(context);
    }
}
