using LinkTracker.Contracts.Rest;

namespace LinkTracker.Bot.App.Infrastructure.Middlewares;

public class GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger, IHostEnvironment environment)
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
                [environment.IsDevelopment() ? ex.StackTrace ?? "" : ""]);

            await context.Response.WriteAsJsonAsync(error);
        }
    }
}