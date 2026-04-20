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

public class FetchPullRequestsStep(IServiceProvider serviceProvider, ILogger<FetchPullRequestsStep> logger, IOptions<GithubPipelineOptions> options)
{
    private readonly GithubPipelineOptions _options = options.Value;

    public async Task<IEnumerable<PullRequestData[]>> ProcessAsync(List<RepositoryData> batch, ITargetBlock<RepositoryData> deletedReposTarget)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var client = scope.ServiceProvider.GetRequiredService<IGithubClient>();

        var pullRequestsOut = new List<PullRequestData>();

        try
        {
            foreach (var repoChunk in batch.Chunk(_options.ParallelRequestsCount))
            {
                var tasks = repoChunk.Select(r => client.GetAllPullRequests(r.Owner, r.Repo, _options.PageSize));
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
                            deletedReposTarget.Post(repo);
                        }
                        else
                        {
                            logger.LogWarning("Failed to fetch PRs for {Owner}/{Repo}", repo.Owner, repo.Repo);
                        }

                        continue;
                    }

                    pullRequestsOut.AddRange(result.Value.Select(pr => new PullRequestData(
                        repo,
                        pr.Number,
                        pr.Title,
                        pr.Username,
                        pr.CreatedAt,
                        pr.Description,
                        pr.Status
                    )));
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while fetching pull requests");
        }

        return pullRequestsOut.Chunk(_options.BatchSize);
    }
}