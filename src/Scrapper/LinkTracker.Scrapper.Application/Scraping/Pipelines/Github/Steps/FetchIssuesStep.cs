using LinkTracker.Scrapper.Application.Scraping.ApiClients.Github;
using LinkTracker.Scrapper.Application.Scraping.ApiClients.Github.Errors;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Tasks.Dataflow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Steps;

public class FetchIssuesStep(IServiceProvider serviceProvider, ILogger<FetchIssuesStep> logger, IOptions<GithubPipelineOptions> options)
{
    private readonly GithubPipelineOptions _options = options.Value;

    public async Task<IEnumerable<IssueData[]>> ProcessAsync(List<RepositoryData> batch, ITargetBlock<RepositoryData> deletedReposTarget)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var client = scope.ServiceProvider.GetRequiredService<IGithubClient>();

        var issuesOut = new List<IssueData>();

        try
        {
            // TODO: переделать под семафор/Parallel.ForEachAsync, чтоб не тормозить батч из-за WhenAll
            foreach (var repoChunk in batch.Chunk(_options.ParallelRequestsCount))
            {
                var tasks = repoChunk.Select(r => client.GetAllIssues(r.Owner, r.Repo, _options.PageSize));
                var results = await Task.WhenAll(tasks);

                for (var i = 0; i < repoChunk.Length; i++)
                {
                    var result = results[i];
                    var repo = repoChunk[i];

                    if (result.IsFailed)
                    {
                        if (result.HasError<RepositoryNotFound>())
                        {
                            logger.LogWarning("Repository Not Found (404) for {Owner}/{Repo}", repo.Owner, repo.Repo);
                            await deletedReposTarget.SendAsync(repo);
                        }
                        else
                        {
                            logger.LogWarning("Failed to fetch Issues for {Owner}/{Repo}", repo.Owner, repo.Repo);
                        }

                        continue;
                    }

                    issuesOut.AddRange(result.Value.Select(issue => new IssueData(
                        repo,
                        issue.Number,
                        issue.Title,
                        issue.Username,
                        issue.CreatedAt,
                        issue.Description,
                        issue.Status
                    )));
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while fetching issues");
        }

        return issuesOut.Chunk(_options.BatchSize);
    }
}