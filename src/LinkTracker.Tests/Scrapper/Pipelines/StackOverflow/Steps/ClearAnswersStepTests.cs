using System.Threading.Tasks.Dataflow;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Notifications;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Results;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Steps;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Store;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace LinkTracker.Tests.Scrapper.Pipelines.StackOverflow.Steps;

public class ClearAnswersStepTests
{
    [Fact]
    public async Task ProcessAsync_RemovesStaleAnswers()
    {
        var store = Substitute.For<IStackOverflowStore>();
        store.RemoveStaleAnswers(Arg.Any<IEnumerable<long>>(), Arg.Any<IEnumerable<long>>()).Returns(Task.CompletedTask);
        store.RemoveAnswersByQuestionIds(Arg.Any<IEnumerable<long>>()).Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddScoped(_ => store);
        await using var provider = services.BuildServiceProvider();

        var sut = new ClearAnswersStep(provider, NullLogger<ClearAnswersStep>.Instance);
        var output = new BufferBlock<NotificationMessage>();

        var question = PipelineTestData.Question(questionId: 101, linkId: 10);
        var answer = PipelineTestData.Answer(question, answerId: 201);
        var batch = new FetchAnswersResult([answer], []);

        var result = await sut.ProcessAsync(batch);

        Assert.Single(result);
        await store.Received(1)
            .RemoveStaleAnswers(Arg.Is<IEnumerable<long>>(ids => ids.SequenceEqual(new long[] { 101L })),
                Arg.Is<IEnumerable<long>>(ids => ids.SequenceEqual(new long[] { 201L })));
    }
}
