using FluentResults;
using LinkTracker.Scrapper.ApiClients.Abstractions;
using LinkTracker.Scrapper.ApiClients.Shared;

namespace LinkTracker.Scrapper.ApiClients.Resources.StackOverflow.Question;

public class StackOverflowQuestionChecker(IStackOverflowClient stackOverflowClient) : ILinkChecker
{
    public string Key => "stackoverflow-question";
    public string SupportedUrlType => "https://stackoverflow.com/questions/{question_id}";

    public bool CanHandle(Uri url)
    {
        if (!url.Host.Equals("stackoverflow.com", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return TryParseQuestionId(url, out _);
    }

    public async Task<Result<UpdateCheckResult>> CheckAsync(Uri url, DateTime? lastChangeAt)
    {
        if (!TryParseQuestionId(url, out var questionId))
        {
            return new UpdateCheckResult(false, null, "Cannot parse question ID from URL");
        }

        var apiResponse = await stackOverflowClient.GetQuestionAsync(questionId);
        if (!apiResponse.IsSuccessful)
        {
            return Result.Fail(apiResponse.Error.Content);
        }

        var response = apiResponse.Content;
        if (response is null || response.Items.Count == 0)
        {
            return new UpdateCheckResult(false, null, "Question not found");
        }

        var question = response.Items[0];
        var lastActivity = DateTimeOffset.FromUnixTimeSeconds(question.LastActivityDate);

        var hasUpdate = lastChangeAt is null || lastActivity > lastChangeAt;
        return new UpdateCheckResult(
            hasUpdate,
            lastActivity,
            hasUpdate
                ? $"Question \"{question.Title}\" was updated (answers: {question.AnswerCount})"
                : "");
    }

    private static bool TryParseQuestionId(Uri url, out long questionId)
    {
        questionId = 0;

        // stackoverflow.com/questions/{id}/...
        var segments = url.Segments
            .Select(s => s.TrimEnd('/'))
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();

        if (segments.Count < 2 ||
            !segments[0].Equals("questions", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return long.TryParse(segments[1], out questionId);
    }
}
