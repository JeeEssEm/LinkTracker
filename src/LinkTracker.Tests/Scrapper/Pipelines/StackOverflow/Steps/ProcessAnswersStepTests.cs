using System.Threading.Tasks.Dataflow;
using LinkTracker.Scrapper.Application.Abstractions;
using LinkTracker.Scrapper.Application.Dtos.Outbox;
using LinkTracker.Scrapper.Application.Repositories;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Notifications;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Steps;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Store;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace LinkTracker.Tests.Scrapper.Pipelines.StackOverflow.Steps;

public class ProcessAnswersStepTests
{
    [Fact]
    public async Task ProcessAsync_NewAnswer_SendsNotificationWithTrimmedPreview()
    {
        var store = Substitute.For<IStackOverflowStore>();
        var subRepo = Substitute.For<ISubscriptionRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        var outboxRepo = Substitute.For<IOutboxEventRepository>();

        var question = PipelineTestData.Question(questionId: 101, linkId: 8, title: "Q title", isFirstCheck: false, chatIds: [1]);
        var answer = PipelineTestData.Answer(question, answerId: 201, body: new string('a', 250), owner: "alice");

        store.CreateAndReturnIfNotExist(Arg.Any<LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Data.AnswerData[]>())
            .Returns([answer]);

        var services = new ServiceCollection();
        services.AddScoped(_ => store);
        services.AddScoped(_ => subRepo);
        services.AddScoped(_ => uow);
        services.AddScoped(_ => outboxRepo);
        await using var provider = services.BuildServiceProvider();

        var sut = new ProcessAnswersStep(provider, NullLogger<ProcessAnswersStep>.Instance);

        await sut.ProcessAsync([answer]);

        await outboxRepo.Received(1).CreateAsync(
            Arg.Is<IEnumerable<CreateOutboxEvent>>(e => e.First().Payload.Contains("Q title")));
    }
}
