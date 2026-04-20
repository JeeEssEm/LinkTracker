using System.Threading.Tasks.Dataflow;
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

        var question = PipelineTestData.Question(questionId: 101, linkId: 8, title: "Q title", isFirstCheck: false, chatIds: [1]);
        var answer = PipelineTestData.Answer(question, answerId: 201, body: new string('a', 250), owner: "alice");

        store.CreateAndReturnIfNotExist(Arg.Any<LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Data.AnswerData[]>())
            .Returns([answer]);

        var services = new ServiceCollection();
        services.AddScoped(_ => store);
        services.AddScoped(_ => subRepo);
        await using var provider = services.BuildServiceProvider();

        var sut = new ProcessAnswersStep(provider, NullLogger<ProcessAnswersStep>.Instance);
        var output = new BufferBlock<NotificationMessage>();

        await sut.ProcessAsync([answer], output);

        Assert.True(output.TryReceive(out var message));
        Assert.Equal(question.Link.Url, message!.Url);
        Assert.Contains("alice", message.Message);
        Assert.DoesNotContain(new string('a', 230), message.Message);
    }
}
