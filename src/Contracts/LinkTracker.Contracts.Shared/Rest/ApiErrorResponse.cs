namespace LinkTracker.Contracts.Rest;

public record ApiErrorResponse(
    string Description,
    string Code,
    string ExceptionName,
    string ExceptionMessage,
    List<string> Stacktrace);
