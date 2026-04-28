using System.Threading.Tasks.Dataflow;
using LinkTracker.Scrapper.Application.Abstractions;
using LinkTracker.Scrapper.Application.Dtos.Outbox;
using LinkTracker.Scrapper.Application.Repositories;
using LinkTracker.Scrapper.Application.Scraping.ApiClients.Github.Models.Statuses;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Steps;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Store;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace LinkTracker.Tests.Scrapper.Pipelines.Github.Steps;

public class ProcessPullRequestsStepTests
{
    [Fact]
    public async Task ProcessAsync_NewPullRequestAndNotFirstCheck_SendsNotification()
    {
        var store = Substitute.For<IGithubStore>();
        var uow = Substitute.For<IUnitOfWork>();
        var outboxRepo = Substitute.For<IOutboxEventRepository>();

        var repo = PipelineTestData.Repository(owner: "octocat", repo: "hello-world", isFirstCheck: false, chatIds: [9]);
        var pr = PipelineTestData.PullRequest(
            repo,
            number: 2,
            title: "New PR",
            username: "alice",
            description: new string('p', 260),
            status: PullRequestStatus.Pending,
            isFirstCheck: false);

        store.CreateAndReturnIfNotExist(Arg.Any<LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Data.PullRequestData[]>())
            .Returns([pr]);

        var services = new ServiceCollection();
        services.AddScoped(_ => store);
        services.AddScoped(_ => uow);
        services.AddScoped(_ => outboxRepo);
        await using var provider = services.BuildServiceProvider();

        var sut = new ProcessPullRequestsStep(provider, NullLogger<ProcessPullRequestsStep>.Instance);

        await sut.ProcessAsync([pr]);

        await outboxRepo.Received(1).CreateAsync(
            Arg.Is<IEnumerable<CreateOutboxEvent>>(e => e.First().Payload.Contains("alice")));
    }
}
