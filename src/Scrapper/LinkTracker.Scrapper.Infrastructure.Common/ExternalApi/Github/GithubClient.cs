using FluentResults;
using LinkTracker.Scrapper.Application.Scraping.ApiClients.Github;
using LinkTracker.Scrapper.Application.Scraping.ApiClients.Github.Models;
using LinkTracker.Scrapper.Application.Scraping.ApiClients.Github.Models.Statuses;
using Microsoft.Extensions.Logging;

namespace LinkTracker.Scrapper.Infrastructure.Pipelines.ExternalApi.Github;

public class GithubClient(IGithubRefitClient refitClient, ILogger<GithubClient> logger) : IGithubClient
{
    public async Task<Result<List<IssueResponse>>> GetIssues(string owner, string repo, int limit, int page)
    {
        var res = await refitClient.GetIssues(owner, repo, limit, page);

        if (res is { IsSuccessful: true, Content: not null })
        {
            var issues = res.Content
                .Select(i => new IssueResponse(
                    i.Title,
                    i.Number,
                    i.User?.Login ?? string.Empty,
                    i.CreatedAt,
                    i.Body ?? string.Empty,
                    i.State == "open" ? IssueStatus.Open : IssueStatus.Closed
                ))
                .ToList();

            return Result.Ok(issues);
        }

        logger.LogError("GitHub API error: {Error}", res.Error);
        return Result.Fail(res.Error?.ReasonPhrase ?? "Unknown GitHub API Error");
    }

    public async Task<Result<List<PullRequestResponse>>> GetPullRequests(string owner, string repo, int limit, int page)
    {
        var res = await refitClient.GetPullRequests(owner, repo, limit, page);

        if (res is { IsSuccessful: true, Content: not null })
        {
            var PRs = res.Content
                .Select(pr => new PullRequestResponse(
                    pr.Title,
                    pr.Number,
                    pr.Body ?? string.Empty,
                    pr.User?.Login ?? string.Empty,
                    pr.CreatedAt,
                    pr.State == "open" ? PullRequestStatus.Pending : PullRequestStatus.Approved // Simplistic mapping, as GitHub uses open/closed and reviews for pending/approved
                ))
                .ToList();

            return Result.Ok(PRs);
        }

        logger.LogError("GitHub API error: {Error}", res.Error);
        return Result.Fail(res.Error?.ReasonPhrase ?? "Unknown GitHub API Error");
    }

    public async Task<Result<List<IssueResponse>>> GetAllIssues(string owner, string repo, int limitPerPage)
    {
        var allIssues = new List<IssueResponse>();
        var page = 1;

        while (true)
        {
            var res = await GetIssues(owner, repo, limitPerPage, page);
            if (res.IsFailed)
            {
                return Result.Fail(res.Errors);
            }

            if (res.Value.Count == 0)
            {
                break;
            }

            allIssues.AddRange(res.Value);
            page++;
        }

        return Result.Ok(allIssues);
    }

    public async Task<Result<List<PullRequestResponse>>> GetAllPullRequests(string owner, string repo, int limitPerPage)
    {
        var allPRs = new List<PullRequestResponse>();
        var page = 1;

        while (true)
        {
            var res = await GetPullRequests(owner, repo, limitPerPage, page);
            if (res.IsFailed)
            {
                return Result.Fail(res.Errors);
            }

            if (res.Value.Count == 0)
            {
                break;
            }

            allPRs.AddRange(res.Value);
            page++;
        }

        return Result.Ok(allPRs);
    }
}