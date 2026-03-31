namespace LinkTracker.Contracts.REST;

public record ApiErrorResponse(
    string Description,
    string Code,
    string ExceptionName,
    string ExceptionMessage,
    List<string> Stacktrace);
