using System.Threading.Tasks.Dataflow;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Data;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Store;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Linq;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using LinkTracker.Scrapper.Application.Repositories;

namespace LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Steps;

public class ProcessDeletedRepositoriesStep(IServiceProvider serviceProvider, ILogger<ProcessDeletedRepositoriesStep> logger)
{
    public async Task ProcessAsync(RepositoryData[] repos, ITargetBlock<NotificationMessage> outputBuffer)
    {
        if (repos.Length == 0)
        {
            return;
        }

        await using var scope = serviceProvider.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IGithubStore>();

        try
        {
            var ids = repos.Select(r => r.Id).Distinct();
            var deletedIds = new List<long>();
            foreach (var id in ids)
            {
                var isDeleted = await store.RemoveRepositoryAsync(id);
                if (isDeleted)
                {
                    deletedIds.Add(id);
                }
            }

            foreach (var repoId in deletedIds)
            {
                var repo = repos.First(r => r.Id == repoId);

                if (repo.Link.ChatIdsToNotify.Count > 0)
                {
                    logger.LogInformation("Repository deleted: {Owner}/{Repo}", repo.Owner, repo.Repo);

                    var text = $"""
                               ⚠️Репозиторий {repo.Owner}/{repo.Repo} удален или закрыт в Github. Подписка удалена.
                               """;

                    var msg = new NotificationMessage(repo.Link.ChatIdsToNotify, text, repo.Link.Url, repo.Link.Id);
                    await outputBuffer.SendAsync(msg);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while processing deleted repositories");
        }
    }
}
