using System.Threading.Tasks.Dataflow;
using LinkTracker.Scrapper.Application.Repositories;
using LinkTracker.Scrapper.Application.Scheduling;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Data;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Store;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Store.ReadModels;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Notifications;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Shared.Data;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Data;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Store;
using LinkTracker.Scrapper.Domain.Tracking.ValueObjects;
using Microsoft.Extensions.DependencyInjection;

namespace LinkTracker.Scrapper.Application.Scraping.Pipelines;

public class ScrapingCoordinator
{
    private readonly StackOverflowPipeline _soPipeline;
    private readonly GithubPipeline _githubPipeline;
    private readonly IServiceProvider _serviceProvider;

    public ScrapingCoordinator(
        StackOverflowPipeline soPipeline,
        GithubPipeline githubPipeline,
        IServiceProvider serviceProvider
        )
    {
        _soPipeline = soPipeline;
        _githubPipeline = githubPipeline;
        _serviceProvider = serviceProvider;
    }

    public async Task RunIterationAsync(int batchSize)
    {
        var now = DateTime.UtcNow;

        await using var scope = _serviceProvider.CreateAsyncScope();
        var linkRepo = scope.ServiceProvider.GetRequiredService<ILinkBulkRepository>();
        var soStore = scope.ServiceProvider.GetRequiredService<IStackOverflowStore>();
        var githubStore = scope.ServiceProvider.GetRequiredService<IGithubStore>();

        var page = 1;
        while (true)
        {
            var links = await linkRepo.GetLinksToCheck(page, batchSize, now);

            // TODO: использовать стратегию
            var soLinks = links.Where(l => l.SourceType == LinkSourceType.StackOverflow).ToList();
            var githubLinks = links.Where(l => l.SourceType == LinkSourceType.Github).ToList();
            if (soLinks.Count == 0 && githubLinks.Count == 0)
            {
                return;
            }

            var soMappings = await soStore.GetQuestionIdsByLinkIdsAsync(soLinks.Select(l => l.Id));
            var githubMappings = await githubStore.GetRepositoriesByLinkIdsAsync(githubLinks.Select(l => l.Id));

            await PushLinksToPipelines(soLinks, soMappings, githubLinks, githubMappings);

            await linkRepo.UpdateCheckDatesAt(
                links.Select(m => m.Id).ToArray());
            page++;
        }
    }

    private async Task PushLinksToPipelines(List<LinkCheckEntry> soLinks, Dictionary<Guid, long> soMappings, List<LinkCheckEntry> githubLinks, Dictionary<Guid, RepositoryModel> githubMappings)
    {
        var questionsBatch = new List<QuestionData>();
        var reposBatch = new List<RepositoryData>();

        foreach (var link in soLinks)
        {
            if (soMappings.TryGetValue(link.Id, out var questionId))
            {
                var isFirstCheck = link.LastCheckAt == null;
                questionsBatch.Add(new QuestionData(
                    questionId,
                    new LinkMetadata(link.Id, link.Url, link.ChatIdsToNotify, isFirstCheck),
                    string.Empty
                ));
            }
        }

        foreach (var link in githubLinks)
        {
            if (githubMappings.TryGetValue(link.Id, out var repoInfo))
            {
                var isFirstCheck = link.LastCheckAt == null;
                reposBatch.Add(new RepositoryData(
                    repoInfo.Id,
                    new LinkMetadata(link.Id, link.Url, link.ChatIdsToNotify, isFirstCheck),
                    repoInfo.Owner,
                    repoInfo.Name
                ));
            }
        }

        if (questionsBatch.Count != 0)
        {
            await _soPipeline.PostBatch(questionsBatch);
        }

        if (reposBatch.Count != 0)
        {
            await _githubPipeline.PostBatch(reposBatch);
        }
    }
}
