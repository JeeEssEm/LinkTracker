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

public class FetchIssuesStepTests
{
    [Fact]
    public async Task ProcessAsync_ReturnsIssuesChunk()
    {
        var client = Substitute.For<IGithubClient>();
        client.GetAllIssues("octocat", "hello-world", 50)
            .Returns(Result.Ok<List<IssueResponse>>([
                new IssueResponse("issue", 10, "alice", DateTime.UtcNow, "body", IssueStatus.Open)
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

        var sut = new FetchIssuesStep(provider, NullLogger<FetchIssuesStep>.Instance, options);
        var deletedRepos = new BufferBlock<RepositoryData>();
        var repo = PipelineTestData.Repository(Guid.NewGuid(), owner: "octocat", repo: "hello-world", chatIds: [1]);

        var result = (await sut.ProcessAsync([repo], deletedRepos)).ToList();

        Assert.Single(result);
        Assert.Single(result[0]);
        Assert.Equal(repo, result[0][0].Repository);
        Assert.False(deletedRepos.TryReceive(out _));
    }
}
