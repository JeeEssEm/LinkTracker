using System.Threading.Tasks.Dataflow;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Notifications;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Data;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Steps;
using System;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace LinkTracker.Scrapper.Application.Scraping.Pipelines.Github;

public class GithubPipeline
{
    private readonly ITargetBlock<List<RepositoryData>> _entryPoint;
    private readonly GithubPipelineOptions _options;

    public ISourceBlock<NotificationMessage> OutputBlock { get; }

    public GithubPipeline(
        FetchIssuesStep fetchIssues,
        ProcessIssuesStep processIssues,
        FetchPullRequestsStep fetchPullRequests,
        ProcessPullRequestsStep processPullRequests,
        ProcessDeletedRepositoriesStep processDeletedRepositories,
        ILogger<GithubPipeline> logger,
        IOptions<GithubPipelineOptions> options)
    {
        _options = options.Value;

        var dataflowOptions = new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = _options.MaxDegreeOfParallelism,
            BoundedCapacity = DataflowBlockOptions.Unbounded,
            EnsureOrdered = false
        };

        var outputBuffer = new BufferBlock<NotificationMessage>(new DataflowBlockOptions
        {
            BoundedCapacity = DataflowBlockOptions.Unbounded,
            EnsureOrdered = false
        });

        var deletedReposBatchBlock = new BatchBlock<RepositoryData>(_options.BatchSize, new GroupingDataflowBlockOptions
        {
            BoundedCapacity = DataflowBlockOptions.Unbounded
        });

        var deletedReposActionBlock = new ActionBlock<RepositoryData[]>(
            async batch => await processDeletedRepositories.ProcessAsync(batch), dataflowOptions);

        deletedReposBatchBlock.LinkTo(deletedReposActionBlock, new DataflowLinkOptions { PropagateCompletion = true });

        var flushTimer = new Timer(_ => deletedReposBatchBlock.TriggerBatch(), null, TimeSpan.FromSeconds(_options.TimerIntervalSeconds), TimeSpan.FromSeconds(_options.TimerIntervalSeconds));

        var broadcastReposBlock = new BroadcastBlock<List<RepositoryData>>(
            repos => repos, new DataflowBlockOptions { BoundedCapacity = DataflowBlockOptions.Unbounded });

        var fetchIssuesBlock = new TransformManyBlock<List<RepositoryData>, IssueData[]>(
            async batch => await fetchIssues.ProcessAsync(batch, deletedReposBatchBlock), dataflowOptions);

        var processIssuesBlock = new ActionBlock<IssueData[]>(
            async result => await processIssues.ProcessAsync(result), dataflowOptions);

        var fetchPullRequestsBlock = new TransformManyBlock<List<RepositoryData>, PullRequestData[]>(
            async batch => await fetchPullRequests.ProcessAsync(batch, deletedReposBatchBlock), dataflowOptions);

        var processPullRequestsBlock = new ActionBlock<PullRequestData[]>(
            async result => await processPullRequests.ProcessAsync(result), dataflowOptions);

        var linkOptions = new DataflowLinkOptions { PropagateCompletion = false };

        // Flow: Repo -> FetchIssues -> ProcessIssues
        broadcastReposBlock.LinkTo(fetchIssuesBlock, linkOptions);
        fetchIssuesBlock.LinkTo(processIssuesBlock, linkOptions);

        // Flow: Repo -> FetchPRs -> ProcessPRs
        broadcastReposBlock.LinkTo(fetchPullRequestsBlock, linkOptions);
        fetchPullRequestsBlock.LinkTo(processPullRequestsBlock, linkOptions);

        void LogFault(Task t)
        {
            if (t is { IsFaulted: true, Exception: not null })
            {
                logger.LogCritical(t.Exception, "Github pipeline block faulted");
            }
        }

        fetchIssuesBlock.Completion.ContinueWith(LogFault);
        processIssuesBlock.Completion.ContinueWith(LogFault);
        fetchPullRequestsBlock.Completion.ContinueWith(LogFault);
        processPullRequestsBlock.Completion.ContinueWith(LogFault);
        deletedReposActionBlock.Completion.ContinueWith(LogFault);

        _entryPoint = broadcastReposBlock;
        OutputBlock = outputBuffer;
    }

    public async Task PostBatch(List<RepositoryData> batch)
    {
        await _entryPoint.SendAsync(batch);
    }
}