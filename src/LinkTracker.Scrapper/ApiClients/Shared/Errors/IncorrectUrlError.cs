using FluentResults;

namespace LinkTracker.Scrapper.ApiClients.Shared.Errors;

public class IncorrectUrlError(string message, List<string> supportedTypes) : Error(message)
{
    public List<string> SupportedTypes { get; } = supportedTypes;
}