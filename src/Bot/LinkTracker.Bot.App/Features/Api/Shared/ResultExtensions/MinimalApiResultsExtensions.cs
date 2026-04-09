using FluentResults;
using LinkTracker.Bot.Contracts.Errors;
using LinkTracker.Contracts.Rest;

namespace LinkTracker.Bot.App.Features.Api.Shared.ResultExtensions;

public static class MinimalApiResultsExtensions
{
    public static IResult ToMinimalApiResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Results.Ok(result.Value);
        }

        return MapErrors(result.Errors);
    }

    public static IResult ToMinimalApiResult(this Result result)
    {
        if (result.IsSuccess)
        {
            return Results.Ok();
        }

        return MapErrors(result.Errors);
    }

    private static IResult MapErrors(IReadOnlyList<IError> errors)
    {
        return Results.BadRequest(new ApiErrorResponse(
            "Incorrect request parameters",
            "400",
            errors.FirstOrDefault(new CannotHandleRequest()).GetType().Name,
            string.Join("; ", errors.Select(e => e.Message)),
            []));
    }
}