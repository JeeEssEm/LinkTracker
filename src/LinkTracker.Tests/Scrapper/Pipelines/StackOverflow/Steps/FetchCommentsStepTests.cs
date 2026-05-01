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

public class FetchCommentsStepTests
{
    [Fact]
    public async Task ProcessAsync_ReturnsCommentsForAnswers()
    {
        var client = Substitute.For<IStackOverflowClient>();
        client.GetComments(Arg.Any<long[]>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(Result.Ok(new CommentsResponse([
                new CommentResponse(401, 201, "comment", "eve", DateTime.UtcNow)
            ], false)));

        var services = new ServiceCollection();
        services.AddScoped(_ => client);
        await using var provider = services.BuildServiceProvider();

        var options = Options.Create(new StackOverflowPipelineOptions());
        var sut = new FetchCommentsStep(provider, NullLogger<FetchCommentsStep>.Instance, options);

        var question = PipelineTestData.Question(questionId: 101, linkId: Guid.NewGuid());
        var answer = PipelineTestData.Answer(question, answerId: 201);

        var result = await sut.ProcessAsync([answer]);

        Assert.Single(result.FoundComments);
        Assert.Empty(result.MissingAnswerIds);
        Assert.Equal(answer.Id, result.FoundComments[0].Answer.Id);
    }
}
