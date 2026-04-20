using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Data;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Store;

namespace LinkTracker.Scrapper.Infrastructure.Pipelines.Repositories;

public abstract class AbstractStackOverflowStore : IStackOverflowStore
{
    private const string InsertAnswersSql = """
        INSERT INTO stackoverflow_answers (id, question_id)
        SELECT * FROM UNNEST(@Ids, @QuestionIds)
        ON CONFLICT (id) DO NOTHING
        RETURNING id;
        """;

    private const string InsertCommentsSql = """
        INSERT INTO stackoverflow_comments (id, answer_id)
        SELECT * FROM UNNEST(@CommentIds, @AnswerIds)
        ON CONFLICT (id) DO NOTHING
        RETURNING id;
        """;

    private const string InsertQuestionCommentsSql = """
        INSERT INTO stackoverflow_question_comments (id, question_id, created_at)
        SELECT * FROM UNNEST(@CommentIds, @QuestionIds, @CreatedAts)
        ON CONFLICT (id) DO NOTHING
        RETURNING id;
        """;

    private const string CreateQuestionSql = """
        INSERT INTO stackoverflow_questions(id, link_id) VALUES (@id, @linkId)                                     
        """;

    protected abstract Task<long[]> ExecuteQueryAsync(string sql, object parameters);
    protected abstract Task ExecuteAsync(string sql, object parameters);
    protected abstract Task<Dictionary<long, long>> GetQuestionIdsByLinkIdsAsyncInternal(IEnumerable<long> linkIds);

    public Task<Dictionary<long, long>> GetQuestionIdsByLinkIdsAsync(IEnumerable<long> linkIds)
    {
        return GetQuestionIdsByLinkIdsAsyncInternal(linkIds);
    }

    public Task CreateQuestion(long linkId, long questionId)
    {
        return ExecuteAsync(CreateQuestionSql, new { id = questionId, linkId });
    }

    public async Task<AnswerData[]> CreateAndReturnIfNotExist(AnswerData[] answers)
    {
        if (answers.Length == 0)
        {
            return [];
        }

        var insertedIds = (await ExecuteQueryAsync(InsertAnswersSql, new
        {
            Ids = answers.Select(a => a.Id).ToArray(),
            QuestionIds = answers.Select(a => a.Question.Id).ToArray()
        })).ToHashSet();

        return answers.Where(a => insertedIds.Contains(a.Id)).ToArray();
    }

    public async Task<CommentData[]> CreateAndReturnIfNotExist(CommentData[] comments)
    {
        if (comments.Length == 0)
        {
            return [];
        }

        var insertedIds = (await ExecuteQueryAsync(InsertCommentsSql, new
        {
            CommentIds = comments.Select(c => c.Id).ToArray(),
            AnswerIds = comments.Select(c => c.Answer.Id).ToArray()
        })).ToHashSet();

        return comments.Where(c => insertedIds.Contains(c.Id)).ToArray();
    }

    public async Task<QuestionCommentData[]> CreateAndReturnIfNotExist(QuestionCommentData[] comments)
    {
        if (comments.Length == 0)
        {
            return [];
        }

        var insertedIds = (await ExecuteQueryAsync(InsertQuestionCommentsSql, new
        {
            CommentIds = comments.Select(c => c.Id).ToArray(),
            QuestionIds = comments.Select(c => c.Question.Id).ToArray(),
            CreatedAts = comments.Select(c => c.CreatedAt).ToArray()
        })).ToHashSet();

        return comments.Where(c => insertedIds.Contains(c.Id)).ToArray();
    }

    public Task RemoveQuestionsByIds(IEnumerable<long> questionIds)
    {
        var ids = questionIds.ToArray();
        if (ids.Length == 0)
        {
            return Task.CompletedTask;
        }

        return ExecuteAsync("DELETE FROM stackoverflow_questions WHERE id = ANY(@Ids)", new { Ids = ids });
    }

    public Task RemoveStaleAnswers(IEnumerable<long> questionIds, IEnumerable<long> currentAnswerIds)
    {
        var qIds = questionIds.ToArray();
        if (qIds.Length == 0)
        {
            return Task.CompletedTask;
        }

        return ExecuteAsync("DELETE FROM stackoverflow_answers WHERE question_id = ANY(@QuestionIds) AND id != ALL(@CurrentAnswerIds)",
            new { QuestionIds = qIds, CurrentAnswerIds = currentAnswerIds.ToArray() });
    }

    public Task RemoveAnswersByQuestionIds(IEnumerable<long> questionIds)
    {
        var ids = questionIds.ToArray();
        if (ids.Length == 0)
        {
            return Task.CompletedTask;
        }

        return ExecuteAsync("DELETE FROM stackoverflow_answers WHERE question_id = ANY(@Ids)", new { Ids = ids });
    }

    public Task RemoveStaleComments(IEnumerable<long> answerIds, IEnumerable<long> currentCommentIds)
    {
        var aIds = answerIds.ToArray();
        if (aIds.Length == 0)
        {
            return Task.CompletedTask;
        }

        return ExecuteAsync("DELETE FROM stackoverflow_comments WHERE answer_id = ANY(@AnswerIds) AND id != ALL(@CurrentCommentIds)",
            new { AnswerIds = aIds, CurrentCommentIds = currentCommentIds.ToArray() });
    }

    public Task RemoveCommentsByAnswerIds(IEnumerable<long> answerIds)
    {
        var ids = answerIds.ToArray();
        if (ids.Length == 0)
        {
            return Task.CompletedTask;
        }

        return ExecuteAsync("DELETE FROM stackoverflow_comments WHERE answer_id = ANY(@Ids)", new { Ids = ids });
    }

    public Task RemoveStaleQuestionComments(IEnumerable<long> questionIds, IEnumerable<long> currentCommentIds)
    {
        var qIds = questionIds.ToArray();
        if (qIds.Length == 0)
        {
            return Task.CompletedTask;
        }

        return ExecuteAsync("DELETE FROM stackoverflow_question_comments WHERE question_id = ANY(@QuestionIds) AND id != ALL(@CurrentCommentIds)",
            new { QuestionIds = qIds, CurrentCommentIds = currentCommentIds.ToArray() });
    }

    public Task RemoveQuestionCommentsByQuestionIds(IEnumerable<long> questionIds)
    {
        var ids = questionIds.ToArray();
        if (ids.Length == 0)
        {
            return Task.CompletedTask;
        }

        return ExecuteAsync("DELETE FROM stackoverflow_question_comments WHERE question_id = ANY(@Ids)", new { Ids = ids });
    }
}
