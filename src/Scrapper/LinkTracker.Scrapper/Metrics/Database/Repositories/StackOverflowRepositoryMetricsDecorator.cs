using System.Diagnostics;
using LinkTracker.Scrapper.Domain.Tracking.Entities;
using LinkTracker.Scrapper.Domain.Tracking.SourceHandlers.StackOverflow;

namespace LinkTracker.Scrapper.Metrics.Database.Repositories;

public class StackOverflowRepositoryMetricsDecorator(IStackOverflowRepository soRepo, ApplicationMetrics metrics) : IStackOverflowRepository
{
    public async Task CreateQuestionAsync(LinkId linkId, long questionId)
    {
        var sw = Stopwatch.StartNew();
        await soRepo.CreateQuestionAsync(linkId, questionId);
        metrics.RequestDuration.Record(
            sw.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("scope", RequestDurationScopes.Database),
            new KeyValuePair<string, object?>("scope_type", "stackoverflow_questions")
        );
    }
}