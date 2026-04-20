using FluentResults;
using LinkTracker.Scrapper.Application.Scraping.ApiClients.StackOverflow;
using LinkTracker.Scrapper.Application.Scraping.ApiClients.StackOverflow.Models;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Steps;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace LinkTracker.Tests.Scrapper.Pipelines.StackOverflow.Steps;

public class FetchQuestionCommentsStepTests
{
    [Fact]
    public async Task ProcessAsync_ReturnsQuestionComments()
    {
        var client = Substitute.For<IStackOverflowClient>();
        client.GetQuestionComments(Arg.Any<long[]>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(Result.Ok(new CommentsResponse([
                new CommentResponse(501, 101, "q-comment", "sam", DateTime.UtcNow)
            ], false)));

        var services = new ServiceCollection();
        services.AddScoped(_ => client);
        await using var provider = services.BuildServiceProvider();

        var options = Options.Create(new StackOverflowPipelineOptions());
        var sut = new FetchQuestionCommentsStep(provider, NullLogger<FetchQuestionCommentsStep>.Instance, options);

        var question = PipelineTestData.Question(questionId: 101, linkId: 3);
        var result = await sut.ProcessAsync([question]);

        Assert.Single(result.ReceivedComments);
        Assert.Empty(result.MissingQuestionIds);
        Assert.Equal(question.Id, result.ReceivedComments[0].Question.Id);
    }
}
