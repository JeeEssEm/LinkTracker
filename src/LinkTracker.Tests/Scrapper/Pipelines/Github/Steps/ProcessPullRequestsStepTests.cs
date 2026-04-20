using System.Threading.Tasks.Dataflow;
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
        await using var provider = services.BuildServiceProvider();

        var sut = new ProcessPullRequestsStep(provider, NullLogger<ProcessPullRequestsStep>.Instance);
        var output = new BufferBlock<NotificationMessage>();

        await sut.ProcessAsync([pr], output);

        Assert.True(output.TryReceive(out var notification));
        Assert.Contains("New PR", notification!.Message);
        Assert.Contains("octocat/hello-world", notification.Message);
        Assert.Equal(repo.Link.Url, notification.Url);
    }
}
