using System.Diagnostics;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Data;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Store;

namespace LinkTracker.Scrapper.Metrics.Database.Repositories;

public class StackOverflowStoreMetricsDecorator(IStackOverflowStore stackOverflowStore, ApplicationMetrics metrics) : IStackOverflowStore
{
    public async Task<AnswerData[]> CreateAndReturnIfNotExist(AnswerData[] answers)
    {
        var sw = Stopwatch.StartNew();
        var result = await stackOverflowStore.CreateAndReturnIfNotExist(answers);
        metrics.RequestDuration.Record(
            sw.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("scope", RequestDurationScopes.Database),
            new KeyValuePair<string, object?>("scope_type", "stackoverflow_answers")
        );
        return result;
    }

    public async Task<CommentData[]> CreateAndReturnIfNotExist(CommentData[] comments)
    {
        var sw = Stopwatch.StartNew();
        var result = await stackOverflowStore.CreateAndReturnIfNotExist(comments);
        metrics.RequestDuration.Record(
            sw.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("scope", RequestDurationScopes.Database),
            new KeyValuePair<string, object?>("scope_type", "stackoverflow_answer_comments")
        );
        return result;
    }

    public async Task<QuestionCommentData[]> CreateAndReturnIfNotExist(QuestionCommentData[] comments)
    {
        var sw = Stopwatch.StartNew();
        var result = await stackOverflowStore.CreateAndReturnIfNotExist(comments);
        metrics.RequestDuration.Record(
            sw.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("scope", RequestDurationScopes.Database),
            new KeyValuePair<string, object?>("scope_type", "stackoverflow_question_comments")
        );
        return result;
    }

    public async Task RemoveStaleAnswers(IEnumerable<long> questionIds, IEnumerable<long> currentAnswerIds)
    {
        var sw = Stopwatch.StartNew();
        await stackOverflowStore.RemoveStaleAnswers(questionIds, currentAnswerIds);
        metrics.RequestDuration.Record(
            sw.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("scope", RequestDurationScopes.Database),
            new KeyValuePair<string, object?>("scope_type", "stackoverflow_answers")
        );
    }

    public async Task RemoveAnswersByQuestionIds(IEnumerable<long> questionIds)
    {
        var sw = Stopwatch.StartNew();
        await stackOverflowStore.RemoveAnswersByQuestionIds(questionIds);
        metrics.RequestDuration.Record(
            sw.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("scope", RequestDurationScopes.Database),
            new KeyValuePair<string, object?>("scope_type", "stackoverflow_answers")
        );
    }

    public async Task RemoveStaleComments(IEnumerable<long> answerIds, IEnumerable<long> currentCommentIds)
    {
        var sw = Stopwatch.StartNew();
        await stackOverflowStore.RemoveStaleComments(answerIds, currentCommentIds);
        metrics.RequestDuration.Record(
            sw.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("scope", RequestDurationScopes.Database),
            new KeyValuePair<string, object?>("scope_type", "stackoverflow_answer_comments")
        );
    }

    public async Task RemoveCommentsByAnswerIds(IEnumerable<long> answerIds)
    {
        var sw = Stopwatch.StartNew();
        await stackOverflowStore.RemoveCommentsByAnswerIds(answerIds);
        metrics.RequestDuration.Record(
            sw.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("scope", RequestDurationScopes.Database),
            new KeyValuePair<string, object?>("scope_type", "stackoverflow_answer_comments")
        );
    }

    public async Task RemoveStaleQuestionComments(IEnumerable<long> questionIds, IEnumerable<long> currentCommentIds)
    {
        var sw = Stopwatch.StartNew();
        await stackOverflowStore.RemoveStaleQuestionComments(questionIds, currentCommentIds);
        metrics.RequestDuration.Record(
            sw.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("scope", RequestDurationScopes.Database),
            new KeyValuePair<string, object?>("scope_type", "stackoverflow_question_comments")
        );
    }

    public async Task RemoveQuestionCommentsByQuestionIds(IEnumerable<long> questionIds)
    {
        var sw = Stopwatch.StartNew();
        await stackOverflowStore.RemoveQuestionCommentsByQuestionIds(questionIds);
        metrics.RequestDuration.Record(
            sw.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("scope", RequestDurationScopes.Database),
            new KeyValuePair<string, object?>("scope_type", "stackoverflow_question_comments")
        );
    }

    public async Task<Dictionary<Guid, long>> GetQuestionIdsByLinkIdsAsync(IEnumerable<Guid> linkIds)
    {
        var sw = Stopwatch.StartNew();
        var result = await stackOverflowStore.GetQuestionIdsByLinkIdsAsync(linkIds);
        metrics.RequestDuration.Record(
            sw.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("scope", RequestDurationScopes.Database),
            new KeyValuePair<string, object?>("scope_type", "stackoverflow_questions")
        );
        return result;
    }
}
