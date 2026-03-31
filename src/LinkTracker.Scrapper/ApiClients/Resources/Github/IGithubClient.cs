using LinkTracker.Scrapper.ApiClients.Resources.Github.Repository;
using Refit;

namespace LinkTracker.Scrapper.ApiClients.Resources.Github;

public interface IGithubClient
{
    [Get("/repos/{owner}/{repo}")]
    Task<SimpleGithubResponse?> GetRepositoryAsync(string owner, string repo);
}
