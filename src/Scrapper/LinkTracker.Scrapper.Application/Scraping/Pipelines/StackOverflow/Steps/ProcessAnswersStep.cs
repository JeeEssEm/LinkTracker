using LinkTracker.Bot.Contracts.Rest;
using LinkTracker.Scrapper.Application.Abstractions.UnitOfWork;
using LinkTracker.Scrapper.Application.Dtos.Outbox;
using LinkTracker.Scrapper.Application.Repositories;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Shared;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Data;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Store;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Steps;

public class ProcessAnswersStep(
    IServiceProvider serviceProvider,
    ILogger<ProcessAnswersStep> logger)
{
    private const int PreviewLength = 200;

    public async Task ProcessAsync(IReadOnlyList<AnswerData> batch)
    {
        if (batch.Count == 0)
        {
            return;
        }

        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var store = scope.ServiceProvider.GetRequiredService<IStackOverflowStore>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var outboxRepo = scope.ServiceProvider.GetRequiredService<IOutboxEventRepository>();

            await uow.BeginAsync();

            var newAnswers = await store.CreateAndReturnIfNotExist(batch.ToArray());

            logger.LogInformation(
                "ProcessAnswers: total={Total}, new={New}",
                batch.Count, newAnswers.Length);

            var events = new List<CreateOutboxEvent>();
            foreach (var answer in newAnswers)
            {
                if (!answer.Question.Link.IsFirstCheck)
                {
                    events.Add(new CreateOutboxEvent(new LinkUpdateRequest(
                        answer.Question.Link.Id,
                        answer.Question.Link.Url,
                        $"""
                         📌 {answer.Question.Link.Url}

                         💬 Новый ответ от {answer.OwnerDisplayName} ({answer.CreatedAt:dd.MM.yyyy HH:mm} UTC)
                         {answer.Question.Title}
                         {TextUtilities.Truncate(answer.Body, PreviewLength)}
                         """,
                        answer.Question.Link.ChatIdsToNotify
                        )));
                }
            }
            await outboxRepo.CreateAsync(events);
            await uow.CommitAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process answers batch");
        }
    }
}