using FluentResults;
using LinkTracker.Scrapper.ApiClients.Abstractions;
using LinkTracker.Scrapper.ApiClients.Shared.Errors;
using LinkTracker.Scrapper.Errors.Links;

namespace LinkTracker.Scrapper.ApiClients;

public class LinkCheckerResolver(IEnumerable<ILinkChecker> checkers)
{
    private readonly Dictionary<string, ILinkChecker> _byKey = checkers.ToDictionary(c => c.Key);
    private readonly List<string> _supportedUrlTypes = checkers.Select(checker => checker.SupportedUrlType).ToList();

    public bool CanHandle(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return checkers.Any(c => c.CanHandle(uri));
    }

    public Result<ILinkChecker> Resolve(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return Result.Fail(new UnsupportedLinkType(_supportedUrlTypes));
        }

        var checker = checkers.FirstOrDefault(c => c.CanHandle(uri));
        if (checker is null)
        {
            return Result.Fail(new UnsupportedLinkType(_supportedUrlTypes));
        }

        return Result.Ok(checker);
    }

    public Result<ILinkChecker> GetByKey(string key)
    {
        if (_byKey.TryGetValue(key, out var checker))
        {
            return Result.Ok(checker);
        }

        return Result.Fail(new FactoryForWatcherNotFoundError());
    }
}