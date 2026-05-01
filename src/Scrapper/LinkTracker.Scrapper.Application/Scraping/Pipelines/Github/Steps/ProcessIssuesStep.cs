using LinkTracker.Bot.Contracts.Rest;
using LinkTracker.Scrapper.Application.Abstractions.UnitOfWork;
using LinkTracker.Scrapper.Application.Dtos.Outbox;
using LinkTracker.Scrapper.Application.Repositories;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Data;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Store;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Steps;

public class ProcessIssuesStep(IServiceProvider serviceProvider, ILogger<ProcessIssuesStep> logger)
{
    private const int PreviewDescriptionLength = 200;

    public async Task ProcessAsync(IssueData[] repoIssues)
    {
        if (repoIssues.Length == 0)
        {
            return;
        }

        await using var scope = serviceProvider.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IGithubStore>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var outboxRepo = scope.ServiceProvider.GetRequiredService<IOutboxEventRepository>();

        try
        {
            await uow.BeginAsync();
            var newIssues = await store.CreateAndReturnIfNotExist(repoIssues);
            var events = new List<CreateOutboxEvent>();

            foreach (var issue in newIssues)
            {
                var repo = issue.Repository;
                var link = repo.Link;

                if (!link.IsFirstCheck && link.ChatIdsToNotify.Count > 0)
                {
                    logger.LogInformation("New issue found: {Title} in {Owner}/{Repo}", issue.Title, repo.Owner, repo.Repo);

                    var text = $"""
                               📌 {repo.Link.Url}
                               {repo.Owner}/{repo.Repo}
                               Новое Issue: {issue.Title} от {issue.Username}
                               {TextUtilities.Truncate(issue.Description, PreviewDescriptionLength)}
                               """;

                    events.Add(new CreateOutboxEvent(new LinkUpdateRequest(link.Id, link.Url, text, link.ChatIdsToNotify)));
                }
            }
            await outboxRepo.CreateAsync(events);
            await uow.CommitAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while processing issues batch");
        }
    }
}
