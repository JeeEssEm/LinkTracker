using System.Threading.Tasks.Dataflow;
using FluentResults;
using LinkTracker.Scrapper.Application.Scraping.ApiClients.Github;
using LinkTracker.Scrapper.Application.Scraping.ApiClients.Github.Models;
using LinkTracker.Scrapper.Application.Scraping.ApiClients.Github.Models.Statuses;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Data;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Steps;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace LinkTracker.Tests.Scrapper.Pipelines.Github.Steps;

public class FetchPullRequestsStepTests
{
    [Fact]
    public async Task ProcessAsync_ReturnsPullRequestsChunk()
    {
        var client = Substitute.For<IGithubClient>();
        client.GetAllPullRequests("octocat", "hello-world", 50)
            .Returns(Result.Ok<List<PullRequestResponse>>([
                new PullRequestResponse("pr", 11, "body", "alice", DateTime.UtcNow, PullRequestStatus.Pending)
            ]));

        var services = new ServiceCollection();
        services.AddScoped(_ => client);
        await using var provider = services.BuildServiceProvider();

        var options = Options.Create(new GithubPipelineOptions
        {
            BatchSize = 20,
            ParallelRequestsCount = 2,
            PageSize = 50
        });

        var sut = new FetchPullRequestsStep(provider, NullLogger<FetchPullRequestsStep>.Instance, options);
        var deletedRepos = new BufferBlock<RepositoryData>();
        var repo = PipelineTestData.Repository(Guid.NewGuid(), owner: "octocat", repo: "hello-world", chatIds: [1]);

        var result = (await sut.ProcessAsync([repo], deletedRepos)).ToList();

        Assert.Single(result);
        Assert.Single(result[0]);
        Assert.Equal(repo, result[0][0].Repository);
        Assert.False(deletedRepos.TryReceive(out _));
    }
}
