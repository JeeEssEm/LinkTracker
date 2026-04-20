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

public class FetchAnswersStepTests
{
    [Fact]
    public async Task ProcessAsync_ReturnsAnswersForBatch()
    {
        var client = Substitute.For<IStackOverflowClient>();
        client.GetAnswers(Arg.Any<long[]>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(Result.Ok(new AnswersResponse([
                new AnswerResponse(101, 101, "answer body", "alice", DateTime.UtcNow)
            ], false)));

        var services = new ServiceCollection();
        services.AddScoped(_ => client);
        await using var provider = services.BuildServiceProvider();

        var options = Options.Create(new StackOverflowPipelineOptions());
        var sut = new FetchAnswersStep(provider, NullLogger<FetchAnswersStep>.Instance, options);

        var question = PipelineTestData.Question(questionId: 101, linkId: 101);
        var batches = (await sut.ProcessAsync([question])).ToList();

        Assert.Single(batches);
        Assert.Single(batches[0].FoundAnswers);
        Assert.Empty(batches[0].MissingQuestionIds);
        Assert.Equal(101, batches[0].FoundAnswers[0].Question.Id);
    }

    [Fact]
    public async Task ProcessAsync_WhenAnswerIdDiffersFromQuestionId_DoesNotMarkQuestionAsMissing()
    {
        var client = Substitute.For<IStackOverflowClient>();
        client.GetAnswers(Arg.Any<long[]>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(Result.Ok(new AnswersResponse([
                new AnswerResponse(101, 501, "answer body", "alice", DateTime.UtcNow)
            ], false)));

        var services = new ServiceCollection();
        services.AddScoped(_ => client);
        await using var provider = services.BuildServiceProvider();

        var options = Options.Create(new StackOverflowPipelineOptions());
        var sut = new FetchAnswersStep(provider, NullLogger<FetchAnswersStep>.Instance, options);

        var question = PipelineTestData.Question(questionId: 101, linkId: 101);
        var batches = (await sut.ProcessAsync([question])).ToList();

        Assert.Single(batches);
        Assert.Empty(batches[0].MissingQuestionIds);
        Assert.Single(batches[0].FoundAnswers);
    }

    [Fact]
    public async Task ProcessAsync_DoesNotSplitAnswersOfSameQuestionAcrossBatches()
    {
        var now = DateTime.UtcNow;
        var client = Substitute.For<IStackOverflowClient>();
        client.GetAnswers(Arg.Any<long[]>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(Result.Ok(new AnswersResponse([
                new AnswerResponse(101, 1001, "answer-1", "alice", now),
                new AnswerResponse(101, 1002, "answer-2", "bob", now)
            ], false)));

        var services = new ServiceCollection();
        services.AddScoped(_ => client);
        await using var provider = services.BuildServiceProvider();

        var options = Options.Create(new StackOverflowPipelineOptions
        {
            AnswersBatchSize = 1
        });
        var sut = new FetchAnswersStep(provider, NullLogger<FetchAnswersStep>.Instance, options);

        var question = PipelineTestData.Question(questionId: 101, linkId: 101);
        var batches = (await sut.ProcessAsync([question])).ToList();

        Assert.Single(batches);
        Assert.Equal(2, batches[0].FoundAnswers.Count);
        Assert.All(batches[0].FoundAnswers, a => Assert.Equal(101, a.Question.Id));
    }
}
