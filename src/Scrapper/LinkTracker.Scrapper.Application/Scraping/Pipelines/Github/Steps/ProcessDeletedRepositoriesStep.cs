using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Data;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Store;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using LinkTracker.Bot.Contracts.Rest;
using LinkTracker.Scrapper.Application.Abstractions;
using LinkTracker.Scrapper.Application.Dtos.Outbox;
using LinkTracker.Scrapper.Application.Repositories;

namespace LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Steps;

public class ProcessDeletedRepositoriesStep(IServiceProvider serviceProvider, ILogger<ProcessDeletedRepositoriesStep> logger)
{
    public async Task ProcessAsync(RepositoryData[] repos)
    {
        if (repos.Length == 0)
        {
            return;
        }

        await using var scope = serviceProvider.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IGithubStore>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var outboxRepo = scope.ServiceProvider.GetRequiredService<IOutboxEventRepository>();

        try
        {
            var ids = repos.Select(r => r.Id).Distinct();
            await uow.BeginAsync();
            var deletedIds = new List<long>();
            foreach (var id in ids)
            {
                var isDeleted = await store.RemoveRepositoryAsync(id);
                if (isDeleted)
                {
                    deletedIds.Add(id);
                }
            }

            var events = new List<CreateOutboxEvent>();
            foreach (var repoId in deletedIds)
            {
                var repo = repos.First(r => r.Id == repoId);

                if (repo.Link.ChatIdsToNotify.Count > 0)
                {
                    logger.LogInformation("Repository deleted: {Owner}/{Repo}", repo.Owner, repo.Repo);

                    var text = $"""
                               {repo.Link}
                               
                               ⚠️Репозиторий {repo.Owner}/{repo.Repo} удален или закрыт в Github. Подписка удалена.
                               """;
                    events.Add(new CreateOutboxEvent(new LinkUpdateRequest(repo.Link.Id, repo.Link.Url, text, repo.Link.ChatIdsToNotify)));
                }
            }

            await outboxRepo.CreateAsync(events);
            await uow.CommitAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while processing deleted repositories");
        }
    }
}
