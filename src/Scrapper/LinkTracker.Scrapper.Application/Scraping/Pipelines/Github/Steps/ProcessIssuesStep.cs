using System.Threading.Tasks.Dataflow;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Data;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Store;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Notifications;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Steps;

public class ProcessIssuesStep(IServiceProvider serviceProvider, ILogger<ProcessIssuesStep> logger)
{
    public async Task ProcessAsync(IssueData[] repoIssues, ITargetBlock<NotificationMessage> outputBuffer)
    {
        if (repoIssues.Length == 0)
        {
            return;
        }

        await using var scope = serviceProvider.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IGithubStore>();

        try
        {
            var newIssues = await store.CreateAndReturnIfNotExist(repoIssues);

            foreach (var issue in newIssues)
            {
                var repo = issue.Repository;
                var link = repo.Link;

                if (!link.IsFirstCheck && link.ChatIdsToNotify.Count > 0)
                {
                    logger.LogInformation("New issue found: {Title} in {Owner}/{Repo}", issue.Title, repo.Owner, repo.Repo);

                    var text = $"""
                               📌 {repo.Owner}/{repo.Repo}

                               Новое Issue: {issue.Title} от {issue.Username}
                               {TextUtilities.Truncate(issue.Description, 200)}
                               """;

                    var msg = new NotificationMessage(link.ChatIdsToNotify, text, link.Url, link.Id);
                    await outputBuffer.SendAsync(msg);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while processing issues batch");
        }
    }
}
