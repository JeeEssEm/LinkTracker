using FluentResults;
using LinkTracker.Scrapper.ApiClients.Abstractions;
using LinkTracker.Scrapper.ApiClients.Shared;

namespace LinkTracker.Scrapper.ApiClients.Resources.Github.Repository;

public class GithubRepositoryChecker(IGithubClient githubClient) : ILinkChecker
{
    public string Key => "github-repository";
    public string SupportedUrlType => "https://github.com/{user-name}/{repository-name}";

    public bool CanHandle(Uri url)
    {
        if (!url.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var segments = url.Segments
            .Select(s => s.TrimEnd('/'))
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();

        return segments.Count >= 2;
    }

    public async Task<Result<UpdateCheckResult>> CheckAsync(Uri url, DateTime? lastChangeAt)
    {
        var segments = url.Segments
            .Select(s => s.TrimEnd('/'))
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();

        var owner = segments[0];
        var repo = segments[1];

        var apiResponse = await githubClient.GetRepositoryAsync(owner, repo);
        if (!apiResponse.IsSuccessful)
        {
            return Result.Fail(apiResponse.Error.Content);
        }

        var response = apiResponse.Content;
        if (response is null)
        {
            return new UpdateCheckResult(false, null, "Repository not found");
        }

        var hasUpdate = lastChangeAt is null || response.LastUpdate > lastChangeAt;
        return new UpdateCheckResult(
            hasUpdate,
            response.LastUpdate,
            hasUpdate ? $"Repository {owner}/{repo} was updated" : "");
    }
}
