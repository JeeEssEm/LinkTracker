using FluentResults;

namespace LinkTracker.Bot.App.ApiClients.Scrapper.Errors.Links;

public class UnsupportedLinkType(List<string> supportedTypes) : Error()
{
    public readonly List<string> SupportedTypes = supportedTypes;
}