using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Steps;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LinkTracker.Tests.Scrapper.Pipelines.Github;

public class GithubPipelineTests
{
    [Fact]
    public void Ctor_CreatesOutputBlock()
    {
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var options = Options.Create(new GithubPipelineOptions
        {
            // Keep timer effectively dormant in tests.
            TimerIntervalSeconds = 3600
        });

        var pipeline = new GithubPipeline(
            new FetchIssuesStep(serviceProvider, NullLogger<FetchIssuesStep>.Instance, options),
            new ProcessIssuesStep(serviceProvider, NullLogger<ProcessIssuesStep>.Instance),
            new FetchPullRequestsStep(serviceProvider, NullLogger<FetchPullRequestsStep>.Instance, options),
            new ProcessPullRequestsStep(serviceProvider, NullLogger<ProcessPullRequestsStep>.Instance),
            new ProcessDeletedRepositoriesStep(serviceProvider, NullLogger<ProcessDeletedRepositoriesStep>.Instance),
            NullLogger<GithubPipeline>.Instance,
            options);

        Assert.NotNull(pipeline.OutputBlock);
    }
}
