using LinkTracker.Scrapper.Application.Scraping.ApiClients.Github.Models.Statuses;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Data;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Shared.Data;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Data;

namespace LinkTracker.Tests.Scrapper.Pipelines;

internal static class PipelineTestData
{
    public static LinkMetadata Link(
        long id = 1,
        string? url = null,
        IReadOnlyList<long>? chatIds = null,
        bool isFirstCheck = false)
        => new(id, url ?? $"https://example.com/{id}", chatIds ?? Array.Empty<long>(), isFirstCheck);

    public static RepositoryData Repository(
        int id = 1,
        string owner = "octocat",
        string repo = "hello-world",
        bool isFirstCheck = false,
        IReadOnlyList<long>? chatIds = null)
        => new(id, Link(id, $"https://github.com/{owner}/{repo}", chatIds, isFirstCheck), owner, repo);

    public static IssueData Issue(
        RepositoryData repo,
        int number = 1,
        string title = "Issue title",
        string username = "alice",
        string description = "Issue description",
        IssueStatus status = IssueStatus.Open)
        => new(repo, number, title, username, DateTime.UtcNow, description, status);

    public static PullRequestData PullRequest(
        RepositoryData repo,
        int number = 1,
        string title = "PR title",
        string username = "alice",
        string description = "PR description",
        PullRequestStatus status = PullRequestStatus.Pending,
        bool isFirstCheck = false)
        => new(repo, number, title, username, DateTime.UtcNow, description, status, isFirstCheck);

    public static QuestionData Question(
        long questionId = 101,
        long linkId = 1,
        string title = "Question title",
        bool isFirstCheck = false,
        IReadOnlyList<long>? chatIds = null)
        => new(questionId, Link(linkId, $"https://stackoverflow.com/questions/{questionId}", chatIds, isFirstCheck), title);

    public static AnswerData Answer(
        QuestionData question,
        long answerId = 201,
        string body = "Answer body",
        string owner = "bob")
        => new(answerId, question, body, owner, DateTime.UtcNow);

    public static CommentData Comment(
        AnswerData answer,
        long commentId = 301,
        string body = "Comment body",
        string owner = "carol")
        => new(commentId, answer, body, owner, DateTime.UtcNow);

    public static QuestionCommentData QuestionComment(
        QuestionData question,
        long commentId = 401,
        string body = "Question comment body",
        string owner = "dave")
        => new(commentId, question, body, owner, DateTime.UtcNow);
}
