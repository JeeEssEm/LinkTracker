namespace LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow;

public class StackOverflowPipelineOptions
{
    public const string Position = nameof(StackOverflowPipelineOptions);

    public int MaxDegreeOfParallelism { get; set; } = 4;
    public int AnswersBatchSize { get; set; } = 50;
    public int StackOverflowLimit { get; set; } = 100;
    public int ApiPageSize { get; set; } = 200;
}
