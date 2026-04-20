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

public class ProcessQuestionCommentsStepTests
{
    [Fact]
    public async Task ProcessAsync_NewQuestionComment_SendsNotification()
    {
        var store = Substitute.For<IStackOverflowStore>();
        var subRepo = Substitute.For<ISubscriptionRepository>();

        var question = PipelineTestData.Question(questionId: 101, linkId: 15, title: "Question", isFirstCheck: false, chatIds: [3]);
        var comment = PipelineTestData.QuestionComment(question, commentId: 707, body: "question comment", owner: "oliver");

        store.RemoveStaleQuestionComments(Arg.Any<IEnumerable<long>>(), Arg.Any<IEnumerable<long>>()).Returns(Task.CompletedTask);
        store.RemoveQuestionCommentsByQuestionIds(Arg.Any<IEnumerable<long>>()).Returns(Task.CompletedTask);
        store.CreateAndReturnIfNotExist(Arg.Any<LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Data.QuestionCommentData[]>())
            .Returns([comment]);

        var services = new ServiceCollection();
        services.AddScoped(_ => store);
        services.AddScoped(_ => subRepo);
        await using var provider = services.BuildServiceProvider();

        var sut = new ProcessQuestionCommentsStep(provider, NullLogger<ProcessQuestionCommentsStep>.Instance);
        var output = new BufferBlock<NotificationMessage>();
        var result = new FetchQuestionCommentsResult([comment], []);

        await sut.ProcessAsync(result, output);

        Assert.True(output.TryReceive(out var message));
        Assert.Equal(question.Link.Url, message!.Url);
        Assert.Contains("oliver", message.Message);
    }
}
