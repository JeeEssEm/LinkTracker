using FluentResults;

namespace LinkTracker.Bot.App.Features.Api.Shared.Errors;

public class IncorrectRequestParametersError(string message) : Error();