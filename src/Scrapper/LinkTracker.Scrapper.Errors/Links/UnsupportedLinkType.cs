using FluentResults;

namespace LinkTracker.Scrapper.Errors.Links;

public class UnsupportedLinkType(List<string> supportedTypes) : Error()
{
    public readonly List<string> SupportedTypes = supportedTypes;
}