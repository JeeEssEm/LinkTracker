using LinkTracker.Contracts.REST;

namespace LinkTracker.Scrapper.Infrastructure.Middleware;

public class GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception for {Method} {Path}",
                context.Request.Method, context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var error = new ApiErrorResponse(
                "Internal server error",
                "500",
                ex.GetType().Name,
                ex.Message,
                [ex.StackTrace ?? ""]);

            await context.Response.WriteAsJsonAsync(error);
        }
    }
}
