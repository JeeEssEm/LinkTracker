using System.Threading.Tasks.Dataflow;
using LinkTracker.Scrapper.Application.Abstractions;
using LinkTracker.Scrapper.Application.Dtos.Outbox;
using LinkTracker.Scrapper.Application.Repositories;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Steps;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Store;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace LinkTracker.Tests.Scrapper.Pipelines.Github.Steps;

public class ProcessDeletedRepositoriesStepTests
{
    [Fact]
    public async Task ProcessAsync_DeletedRepository_SendsNotification()
    {
        var store = Substitute.For<IGithubStore>();
        var uow = Substitute.For<IUnitOfWork>();
        var outboxRepo = Substitute.For<IOutboxEventRepository>();
        store.RemoveRepositoryAsync(1).Returns(true);

        var services = new ServiceCollection();
        services.AddScoped(_ => store);
        services.AddScoped(_ => uow);
        services.AddScoped(_ => outboxRepo);

        await using var provider = services.BuildServiceProvider();

        var sut = new ProcessDeletedRepositoriesStep(provider, NullLogger<ProcessDeletedRepositoriesStep>.Instance);
        var repo = PipelineTestData.Repository(id: 1, owner: "octocat", repo: "hello-world", chatIds: [1]);

        await sut.ProcessAsync([repo]);

        await store.Received(1).RemoveRepositoryAsync(1);
        await outboxRepo.Received(1).CreateAsync(
            Arg.Is<IEnumerable<CreateOutboxEvent>>(e => e.First().Payload.Contains("octocat")));
    }
}
