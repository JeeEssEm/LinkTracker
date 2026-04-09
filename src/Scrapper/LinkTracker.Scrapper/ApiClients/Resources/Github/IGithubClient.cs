using LinkTracker.Scrapper.ApiClients.Resources.Github.Repository;
using Refit;

namespace LinkTracker.Scrapper.ApiClients.Resources.Github;

public interface IGithubClient
{
    [Headers("User-Agent: refit-client")]
    [Get("/repos/{owner}/{repo}")]
    Task<ApiResponse<SimpleGithubResponse>> GetRepositoryAsync(string owner, string repo);
}
