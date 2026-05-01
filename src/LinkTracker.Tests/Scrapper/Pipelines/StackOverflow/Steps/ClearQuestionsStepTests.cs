using System.Threading.Tasks.Dataflow;
using LinkTracker.Scrapper.Application.Abstractions.UnitOfWork;
using LinkTracker.Scrapper.Domain.Tracking.Repositories;
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
        var linkRepo = Substitute.For<ILinkBulkRepository>();
        var subRepo = Substitute.For<ISubscriptionRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        var outboxRepo = Substitute.For<IOutboxEventRepository>();

        uow.BeginAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        uow.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddScoped(_ => store);
        services.AddScoped(_ => linkRepo);
        services.AddScoped(_ => subRepo);
        services.AddScoped(_ => uow);
        services.AddScoped(_ => outboxRepo);
        await using var provider = services.BuildServiceProvider();

        var sut = new ClearQuestionsStep(provider, NullLogger<ClearQuestionsStep>.Instance);

        var id = Guid.NewGuid();
        var banned = PipelineTestData.Question(questionId: 101, linkId: id, chatIds: [1]);
        var result = new FetchQuestionsResult([], [banned]);

        var remaining = await sut.ProcessAsync(result);

        Assert.Empty(remaining);
        await linkRepo.Received(1).RemoveLinksAsync(Arg.Is<IEnumerable<Guid>>(ids => ids.SequenceEqual(new[] { id })));
    }
}
