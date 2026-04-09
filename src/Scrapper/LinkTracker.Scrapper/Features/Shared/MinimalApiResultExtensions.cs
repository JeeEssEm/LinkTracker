using FluentResults;
using LinkTracker.Contracts.Rest;
using LinkTracker.Scrapper.Errors.Chats;
using LinkTracker.Scrapper.Errors.Links;
using LinkTracker.Scrapper.Features.Shared.Errors;

namespace LinkTracker.Scrapper.Features.Shared;

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

        if (error is ChatAlreadyExists or LinkAlreadyExists)
        {
            return Results.Conflict(new ApiErrorResponse(
                error.Message, "409", error.GetType().Name, error.Message, []));
        }

        if (error is ChatNotFound or LinkNotFound)
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