using FluentResults;
using LinkTracker.Scrapper.ApiClients.Shared;

namespace LinkTracker.Scrapper.ApiClients.Abstractions;

public interface ILinkChecker
{
    string Key { get; }
    string SupportedUrlType { get; }
    bool CanHandle(Uri url);
    Task<Result<UpdateCheckResult>> CheckAsync(Uri url, DateTime? lastChangeAt);
}
