using System.Diagnostics;

namespace LinkTracker.Scrapper.Infrastructure.Middleware;

public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = Guid.NewGuid().ToString("N")[..8];

        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["RequestId"] = requestId,
            ["RequestMethod"] = context.Request.Method,
            ["RequestPath"] = context.Request.Path.ToString()
        }))
        {
            logger.LogInformation("Incoming request {Method} {Path}",
                context.Request.Method, context.Request.Path);

            var sw = Stopwatch.StartNew();

            await next(context);

            sw.Stop();
            logger.LogInformation("Response {StatusCode} in {ElapsedMs}ms",
                context.Response.StatusCode, sw.ElapsedMilliseconds);
        }
    }
}
