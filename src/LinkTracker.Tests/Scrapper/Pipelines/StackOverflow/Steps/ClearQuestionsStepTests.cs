using System.Threading.Tasks.Dataflow;
using LinkTracker.Scrapper.Application.Abstractions;
using LinkTracker.Scrapper.Application.Repositories;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Notifications;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Results;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Steps;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Store;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace LinkTracker.Tests.Scrapper.Pipelines.StackOverflow.Steps;

public class ClearQuestionsStepTests
{
    [Fact]
    public async Task ProcessAsync_BannedQuestions_AreRemovedAndNotified()
    {
        var store = Substitute.For<IStackOverflowStore>();
        var linkRepo = Substitute.For<ILinkRepository>();
        var subRepo = Substitute.For<ISubscriptionRepository>();
        var uow = Substitute.For<IUnitOfWork>();

        store.RemoveQuestionsByIds(Arg.Any<IEnumerable<long>>()).Returns(Task.CompletedTask);
        linkRepo.RemoveLinksAsync(Arg.Any<IEnumerable<long>>()).Returns(Task.CompletedTask);
        uow.BeginAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        uow.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddScoped(_ => store);
        services.AddScoped(_ => linkRepo);
        services.AddScoped(_ => subRepo);
        services.AddScoped(_ => uow);
        await using var provider = services.BuildServiceProvider();

        var sut = new ClearQuestionsStep(provider, NullLogger<ClearQuestionsStep>.Instance);
        var output = new BufferBlock<NotificationMessage>();

        var banned = PipelineTestData.Question(questionId: 101, linkId: 11, chatIds: [1]);
        var result = new FetchQuestionsResult([], [banned]);

        var remaining = await sut.ProcessAsync(result, output);

        Assert.Empty(remaining);
        await store.Received(1).RemoveQuestionsByIds(Arg.Is<IEnumerable<long>>(ids => ids.SequenceEqual(new long[] { 101 })));
        await linkRepo.Received(1).RemoveLinksAsync(Arg.Is<IEnumerable<long>>(ids => ids.SequenceEqual(new long[] { 11L })));
        Assert.True(output.TryReceive(out var message));
        Assert.Equal(11, message!.LinkId);
    }
}
