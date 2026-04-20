using System.Threading.Tasks.Dataflow;
using LinkTracker.Scrapper.Application.Repositories;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Notifications;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Results;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Steps;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Store;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace LinkTracker.Tests.Scrapper.Pipelines.StackOverflow.Steps;

public class ProcessCommentsStepTests
{
    [Fact]
    public async Task ProcessAsync_NewComment_SendsNotification()
    {
        var store = Substitute.For<IStackOverflowStore>();
        var subRepo = Substitute.For<ISubscriptionRepository>();

        var question = PipelineTestData.Question(questionId: 101, linkId: 6, title: "Q title", isFirstCheck: false, chatIds: [2]);
        var answer = PipelineTestData.Answer(question, answerId: 202);
        var comment = PipelineTestData.Comment(answer, commentId: 303, body: "comment body", owner: "kate");

        store.RemoveStaleComments(Arg.Any<IEnumerable<long>>(), Arg.Any<IEnumerable<long>>()).Returns(Task.CompletedTask);
        store.RemoveCommentsByAnswerIds(Arg.Any<IEnumerable<long>>()).Returns(Task.CompletedTask);
        store.CreateAndReturnIfNotExist(Arg.Any<LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Data.CommentData[]>())
            .Returns([comment]);

        var services = new ServiceCollection();
        services.AddScoped(_ => store);
        services.AddScoped(_ => subRepo);
        await using var provider = services.BuildServiceProvider();

        var sut = new ProcessCommentsStep(provider, NullLogger<ProcessCommentsStep>.Instance);
        var output = new BufferBlock<NotificationMessage>();
        var result = new FetchCommentsResult([comment], []);

        await sut.ProcessAsync(result, output);

        Assert.True(output.TryReceive(out var message));
        Assert.Equal(question.Link.Url, message!.Url);
        Assert.Contains("kate", message.Message);
    }
}
