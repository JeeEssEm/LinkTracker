using LinkTracker.Scrapper.Application.Repositories;
using LinkTracker.Scrapper.Application.Scheduling;
using LinkTracker.Scrapper.Application.Scraping.Pipelines;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Steps;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Store;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Notifications;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Steps;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Store;
using LinkTracker.Scrapper.Configurations;
using LinkTracker.Scrapper.Scheduling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Quartz;

namespace LinkTracker.Tests.Scrapper.Scheduling;

public class LinkCheckJobTests
{
    [Fact]
    public async Task Execute_UsesConfiguredBatchSize()
    {
        // Arrange
        var linkRepo = Substitute.For<ILinkRepository>();
        var soStore = Substitute.For<IStackOverflowStore>();
        var githubStore = Substitute.For<IGithubStore>();

        linkRepo.GetLinksToCheck(1, 7, Arg.Any<DateTime>())
            .Returns(new List<LinkCheckEntry>());

        var services = new ServiceCollection();
        services.AddSingleton(linkRepo);
        services.AddSingleton(soStore);
        services.AddSingleton(githubStore);

        using var serviceProvider = services.BuildServiceProvider();

        var stackOverflowOptions = Options.Create(new StackOverflowPipelineOptions());
        var githubOptions = Options.Create(new GithubPipelineOptions
        {
            // Keep timer inert in tests.
            TimerIntervalSeconds = 3600
        });

        var soPipeline = BuildStackOverflowPipeline(serviceProvider, stackOverflowOptions);
        var githubPipeline = BuildGithubPipeline(serviceProvider, githubOptions);
        var aggregator = new NotificationAggregator(NullLogger<NotificationAggregator>.Instance, serviceProvider);
        var coordinator = new ScrapingCoordinator(soPipeline, githubPipeline, aggregator, serviceProvider);

        var job = new LinkCheckJob(
            Options.Create(new SchedulerOptions { BatchSize = 7 }),
            coordinator);

        var context = Substitute.For<IJobExecutionContext>();

        // Act
        await job.Execute(context);

        // Assert
        await linkRepo.Received(1).GetLinksToCheck(1, 7, Arg.Any<DateTime>());
    }

    private static StackOverflowPipeline BuildStackOverflowPipeline(
        IServiceProvider serviceProvider,
        IOptions<StackOverflowPipelineOptions> options)
    {
        var fetchQuestions = new FetchQuestionsStep(serviceProvider, NullLogger<FetchQuestionsStep>.Instance, options);
        var clearQuestions = new ClearQuestionsStep(serviceProvider, NullLogger<ClearQuestionsStep>.Instance);
        var fetchAnswers = new FetchAnswersStep(serviceProvider, NullLogger<FetchAnswersStep>.Instance, options);
        var clearAnswers = new ClearAnswersStep(serviceProvider, NullLogger<ClearAnswersStep>.Instance);
        var processAnswers = new ProcessAnswersStep(serviceProvider, NullLogger<ProcessAnswersStep>.Instance);
        var fetchComments = new FetchCommentsStep(serviceProvider, NullLogger<FetchCommentsStep>.Instance, options);
        var processComments = new ProcessCommentsStep(serviceProvider, NullLogger<ProcessCommentsStep>.Instance);
        var fetchQuestionComments = new FetchQuestionCommentsStep(serviceProvider, NullLogger<FetchQuestionCommentsStep>.Instance, options);
        var processQuestionComments = new ProcessQuestionCommentsStep(serviceProvider, NullLogger<ProcessQuestionCommentsStep>.Instance);

        return new StackOverflowPipeline(
            fetchQuestions,
            clearQuestions,
            fetchAnswers,
            clearAnswers,
            processAnswers,
            fetchComments,
            processComments,
            fetchQuestionComments,
            processQuestionComments,
            options,
            NullLogger<StackOverflowPipeline>.Instance);
    }

    private static GithubPipeline BuildGithubPipeline(
        IServiceProvider serviceProvider,
        IOptions<GithubPipelineOptions> options)
    {
        var fetchIssues = new FetchIssuesStep(serviceProvider, NullLogger<FetchIssuesStep>.Instance, options);
        var processIssues = new ProcessIssuesStep(serviceProvider, NullLogger<ProcessIssuesStep>.Instance);
        var fetchPullRequests = new FetchPullRequestsStep(serviceProvider, NullLogger<FetchPullRequestsStep>.Instance, options);
        var processPullRequests = new ProcessPullRequestsStep(serviceProvider, NullLogger<ProcessPullRequestsStep>.Instance);
        var processDeletedRepositories = new ProcessDeletedRepositoriesStep(serviceProvider, NullLogger<ProcessDeletedRepositoriesStep>.Instance);

        return new GithubPipeline(
            fetchIssues,
            processIssues,
            fetchPullRequests,
            processPullRequests,
            processDeletedRepositories,
            NullLogger<GithubPipeline>.Instance,
            options);
    }
}
