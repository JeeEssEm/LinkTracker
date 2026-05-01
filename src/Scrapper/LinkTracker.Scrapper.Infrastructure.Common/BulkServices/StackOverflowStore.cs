using Dapper;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Data;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Store;
using LinkTracker.Scrapper.Infrastructure.Pipelines.Database;

namespace LinkTracker.Scrapper.Infrastructure.Pipelines.BulkServices;

public class StackOverflowStore(DatabaseSession dbSession) : IStackOverflowStore
{
    public async Task<AnswerData[]> CreateAndReturnIfNotExist(AnswerData[] answers)
    {
        if (answers.Length == 0)
        {
            return [];
        }
        var query =
            """
            INSERT INTO stackoverflow_answers (id, question_id)
            SELECT * FROM UNNEST(@Ids, @QuestionIds)
            ON CONFLICT (id) DO NOTHING
            RETURNING id;        
            """;
        var connection = await dbSession.OpenConnectionAsync();
        var result = await connection.QueryAsync<long>(query,
            new
            {
                Ids = answers.Select(a => a.Id).ToArray(),
                QuestionIds = answers.Select(a => a.Question.Id).ToArray()
            },
            transaction: dbSession.Transaction
        );
        var insertedIds = result.ToHashSet();
        return answers.Where(a => insertedIds.Contains(a.Id)).ToArray();
    }

    public async Task<CommentData[]> CreateAndReturnIfNotExist(CommentData[] comments)
    {
        if (comments.Length == 0)
        {
            return [];
        }

        var query =
            """
            INSERT INTO stackoverflow_comments (id, answer_id)
            SELECT * FROM UNNEST(@CommentIds, @AnswerIds)
            ON CONFLICT (id) DO NOTHING
            RETURNING id;
            """;
        var connection = await dbSession.OpenConnectionAsync();
        var result = await connection.QueryAsync<long>(query,
            new
            {
                CommentIds = comments.Select(c => c.Id).ToArray(),
                AnswerIds = comments.Select(c => c.Answer.Id).ToArray()
            },
            transaction: dbSession.Transaction
        );
        var insertedIds = result.ToHashSet();
        return comments.Where(c => insertedIds.Contains(c.Id)).ToArray();
    }

    public async Task<QuestionCommentData[]> CreateAndReturnIfNotExist(QuestionCommentData[] comments)
    {
        if (comments.Length == 0)
        {
            return [];
        }

        var query =
            """
            INSERT INTO stackoverflow_question_comments (id, question_id, created_at)
            SELECT * FROM UNNEST(@CommentIds, @QuestionIds, @CreatedAts)
            ON CONFLICT (id) DO NOTHING
            RETURNING id;
            """;
        var connection = await dbSession.OpenConnectionAsync();
        var result = await connection.QueryAsync<long>(query,
            new
            {
                CommentIds = comments.Select(c => c.Id).ToArray(),
                QuestionIds = comments.Select(c => c.Question.Id).ToArray(),
                CreatedAts = comments.Select(c => c.CreatedAt).ToArray()
            },
            transaction: dbSession.Transaction
        );
        var insertedIds = result.ToHashSet();

        return comments.Where(c => insertedIds.Contains(c.Id)).ToArray();
    }

    public async Task RemoveStaleAnswers(IEnumerable<long> questionIds, IEnumerable<long> currentAnswerIds)
    {
        var qIds = questionIds.ToArray();
        if (qIds.Length == 0)
        {
            return;
        }

        var query = "DELETE FROM stackoverflow_answers WHERE question_id = ANY(@QuestionIds) AND id != ALL(@CurrentAnswerIds)";

        var connection = await dbSession.OpenConnectionAsync();
        await connection.ExecuteAsync(query, new
        {
            QuestionIds = qIds,
            CurrentAnswerIds = currentAnswerIds.ToArray()
        }, transaction: dbSession.Transaction);
    }

    public async Task RemoveAnswersByQuestionIds(IEnumerable<long> questionIds)
    {
        var ids = questionIds.ToArray();
        if (ids.Length == 0)
        {
            return;
        }

        var query = "DELETE FROM stackoverflow_answers WHERE question_id = ANY(@Ids)";
        var connection = await dbSession.OpenConnectionAsync();
        await connection.ExecuteAsync(query,
            new { Ids = ids },
            transaction: dbSession.Transaction
        );
    }

    public async Task RemoveStaleComments(IEnumerable<long> answerIds, IEnumerable<long> currentCommentIds)
    {
        var aIds = answerIds.ToArray();
        if (aIds.Length == 0)
        {
            return;
        }

        var query = """
                    DELETE FROM stackoverflow_comments
                    WHERE answer_id = ANY(@AnswerIds) AND id != ALL(@CurrentCommentIds)
                    """;
        var connection = await dbSession.OpenConnectionAsync();
        await connection.ExecuteAsync(query, new { AnswerIds = aIds, CurrentCommentIds = currentCommentIds.ToArray() },
            transaction: dbSession.Transaction);
    }

    public async Task RemoveCommentsByAnswerIds(IEnumerable<long> answerIds)
    {
        var ids = answerIds.ToArray();
        if (ids.Length == 0)
        {
            return;
        }

        var query = """DELETE FROM stackoverflow_comments WHERE answer_id = ANY(@Ids)""";

        var connection = await dbSession.OpenConnectionAsync();
        await connection.ExecuteAsync(query, new { Ids = ids }, transaction: dbSession.Transaction);
    }

    public async Task RemoveStaleQuestionComments(IEnumerable<long> questionIds, IEnumerable<long> currentCommentIds)
    {
        var qIds = questionIds.ToArray();
        if (qIds.Length == 0)
        {
            return;
        }

        var query = """DELETE FROM stackoverflow_question_comments WHERE question_id = ANY(@QuestionIds) AND id != ALL(@CurrentCommentIds)""";

        var connection = await dbSession.OpenConnectionAsync();
        await connection.ExecuteAsync(query, new { QuestionIds = qIds, CurrentCommentIds = currentCommentIds.ToArray() }, transaction: dbSession.Transaction);
    }

    public async Task RemoveQuestionCommentsByQuestionIds(IEnumerable<long> questionIds)
    {
        var ids = questionIds.ToArray();
        if (ids.Length == 0)
        {
            return;
        }

        var query = """DELETE FROM stackoverflow_question_comments WHERE question_id = ANY(@Ids)""";

        var connection = await dbSession.OpenConnectionAsync();
        await connection.ExecuteAsync(query, new { Ids = ids }, transaction: dbSession.Transaction);
    }

    public async Task<Dictionary<Guid, long>> GetQuestionIdsByLinkIdsAsync(IEnumerable<Guid> linkIds)
    {
        var query = "SELECT link_id as Key, id as Value FROM stackoverflow_questions WHERE link_id = ANY(@LinkIds)";
        var connection = await dbSession.OpenConnectionAsync();
        var results = await connection.QueryAsync<KeyValuePair<Guid, long>>(query, new { LinkIds = linkIds.ToArray() }, transaction: dbSession.Transaction);

        return results.ToDictionary(k => k.Key, v => v.Value);
    }
}