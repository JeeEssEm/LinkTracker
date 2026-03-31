using FluentResults;
using LinkTracker.Contracts.REST;
using LinkTracker.Scrapper.Features.Shared.Errors.Chat;
using LinkTracker.Scrapper.Features.Shared.Errors.Link;

namespace LinkTracker.Scrapper.Features.Shared.Errors;

public static class MinimalApiResultExtensions
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
        var error = errors.FirstOrDefault();

        if (error is ChatAlreadyExistsError or LinkAlreadyExistsError)
        {
            return Results.Conflict(new ApiErrorResponse(
                error.Message, "409", error.GetType().Name, error.Message, []));
        }

        if (error is ChatNotFoundError or LinkNotFoundError)
        {
            return Results.NotFound(new ApiErrorResponse(
                error.Message, "404", error.GetType().Name, error.Message, []));
        }

        return Results.BadRequest(new ApiErrorResponse(
            "Incorrect request parameters",
            "400",
            errors.FirstOrDefault(new HandlerNotFoundError()).GetType().Name,
            string.Join("; ", errors.Select(e => e.Message)),
            []));
    }
}