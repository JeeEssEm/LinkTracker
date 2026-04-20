using System.Threading.Tasks.Dataflow;
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
        store.RemoveRepositoryAsync(1).Returns(true);

        var services = new ServiceCollection();
        services.AddScoped(_ => store);
        await using var provider = services.BuildServiceProvider();

        var sut = new ProcessDeletedRepositoriesStep(provider, NullLogger<ProcessDeletedRepositoriesStep>.Instance);
        var output = new BufferBlock<NotificationMessage>();
        var repo = PipelineTestData.Repository(id: 1, owner: "octocat", repo: "hello-world", chatIds: [1]);

        await sut.ProcessAsync([repo], output);

        await store.Received(1).RemoveRepositoryAsync(1);
        Assert.True(output.TryReceive(out var notification));
        Assert.Equal(repo.Link.Id, notification!.LinkId);
        Assert.Equal(repo.Link.Url, notification.Url);
        Assert.Contains(1, notification.UserIdsToNotify);
    }
}
