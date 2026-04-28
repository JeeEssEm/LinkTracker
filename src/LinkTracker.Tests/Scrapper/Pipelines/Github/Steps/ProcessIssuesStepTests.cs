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

public class ProcessIssuesStepTests
{
    [Fact]
    public async Task ProcessAsync_NewIssueAndNotFirstCheck_SendsNotification()
    {
        var store = Substitute.For<IGithubStore>();
        var uow = Substitute.For<IUnitOfWork>();
        var outboxRepo = Substitute.For<IOutboxEventRepository>();

        var repo = PipelineTestData.Repository(owner: "octocat", repo: "hello-world", isFirstCheck: false, chatIds: [7]);
        var issue = PipelineTestData.Issue(
            repo,
            number: 1,
            title: "New issue",
            username: "alice",
            description: new string('x', 260),
            status: IssueStatus.Open);

        store.CreateAndReturnIfNotExist(Arg.Any<LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Data.IssueData[]>())
            .Returns([issue]);

        var services = new ServiceCollection();
        services.AddScoped(_ => store);
        services.AddScoped(_ => uow);
        services.AddScoped(_ => outboxRepo);
        await using var provider = services.BuildServiceProvider();

        var sut = new ProcessIssuesStep(provider, NullLogger<ProcessIssuesStep>.Instance);

        await sut.ProcessAsync([issue]);

        await outboxRepo.Received(1).CreateAsync(
            Arg.Is<IEnumerable<CreateOutboxEvent>>(e => e.First().Payload.Contains("alice")));
    }
}
