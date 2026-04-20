using LinkTracker.Scrapper.Infrastructure.Pipelines.ExternalApi.Github.Dtos;
using Refit;

namespace LinkTracker.Scrapper.Infrastructure.Pipelines.ExternalApi.Github;

public interface IGithubRefitClient
{
    [Headers("User-Agent: LinkTracker", "Accept: application/vnd.github.v3+json")]
    [Get("/repos/{owner}/{repo}/issues")]
    Task<ApiResponse<List<IssueDto>>> GetIssues(
        [AliasAs("owner")] string owner,
        [AliasAs("repo")] string repo,
        [Query] int per_page,
        [Query] int page,
        [Query] string state = "all");

    [Headers("User-Agent: LinkTracker", "Accept: application/vnd.github.v3+json")]
    [Get("/repos/{owner}/{repo}/pulls")]
    Task<ApiResponse<List<PullRequestDto>>> GetPullRequests(
        [AliasAs("owner")] string owner,
        [AliasAs("repo")] string repo,
        [Query] int per_page,
        [Query] int page,
        [Query] string state = "all");
}
