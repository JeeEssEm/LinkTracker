namespace LinkTracker.Scrapper.Application.Scraping.Pipelines.Github;

public class GithubPipelineOptions
{
    public const string Position = nameof(GithubPipelineOptions);

    public int MaxDegreeOfParallelism { get; set; } = 2;
    public int BatchSize { get; set; } = 100;
    public int TimerIntervalSeconds { get; set; } = 30;
    public int ParallelRequestsCount { get; set; } = 2;
    public int PageSize { get; set; } = 100;
}