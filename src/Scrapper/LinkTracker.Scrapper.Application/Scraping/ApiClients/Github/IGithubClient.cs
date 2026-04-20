using FluentResults;
using LinkTracker.Scrapper.Application.Scraping.ApiClients.Github.Models;

namespace LinkTracker.Scrapper.Application.Scraping.ApiClients.Github;

public interface IGithubClient
{
    Task<Result<List<IssueResponse>>> GetIssues(string owner, string repo, int limit, int page);
    Task<Result<List<PullRequestResponse>>> GetPullRequests(string owner, string repo, int limit, int page);

    Task<Result<List<IssueResponse>>> GetAllIssues(string owner, string repo, int limitPerPage);
    Task<Result<List<PullRequestResponse>>> GetAllPullRequests(string owner, string repo, int limitPerPage);
}
