using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Data;

namespace LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Store;

public interface IStackOverflowStore
{
    Task<AnswerData[]> CreateAndReturnIfNotExist(AnswerData[] answers);

    Task<CommentData[]> CreateAndReturnIfNotExist(CommentData[] comments);

    Task<QuestionCommentData[]> CreateAndReturnIfNotExist(QuestionCommentData[] comments);

    /// <summary>
    /// Удаляет из БД ответы для указанных вопросов, чьи ID не входят в currentAnswerIds.
    /// Используется когда вопрос жив, но часть его ответов удалена.
    /// </summary>
    Task RemoveStaleAnswers(IEnumerable<long> questionIds, IEnumerable<long> currentAnswerIds);
    Task RemoveAnswersByQuestionIds(IEnumerable<long> questionIds);

    /// <summary>
    /// Удаляет из БД комментарии для указанных ответов, чьи ID не входят в currentCommentIds.
    /// Используется когда ответ жив, но часть его комментариев удалена.
    /// </summary>
    Task RemoveStaleComments(IEnumerable<long> answerIds, IEnumerable<long> currentCommentIds);
    Task RemoveCommentsByAnswerIds(IEnumerable<long> answerIds);

    Task RemoveStaleQuestionComments(IEnumerable<long> questionIds, IEnumerable<long> currentCommentIds);
    Task RemoveQuestionCommentsByQuestionIds(IEnumerable<long> questionIds);

    Task<Dictionary<Guid, long>> GetQuestionIdsByLinkIdsAsync(IEnumerable<Guid> linkIds);
}
