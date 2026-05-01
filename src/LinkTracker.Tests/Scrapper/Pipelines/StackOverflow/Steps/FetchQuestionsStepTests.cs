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

public class FetchQuestionsStepTests
{
    [Fact]
    public async Task ProcessAsync_SplitsFoundAndBannedQuestions()
    {
        var client = Substitute.For<IStackOverflowClient>();
        client.GetQuestions(Arg.Any<long[]>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(Result.Ok(new QuestionsResponse([
                new QuestionResponse(101, "How to test")
            ], false)));

        var services = new ServiceCollection();
        services.AddScoped(_ => client);
        await using var provider = services.BuildServiceProvider();

        var options = Options.Create(new StackOverflowPipelineOptions());
        var sut = new FetchQuestionsStep(provider, NullLogger<FetchQuestionsStep>.Instance, options);

        var batch = new List<LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Data.QuestionData>
        {
            PipelineTestData.Question(questionId: 101, linkId: Guid.NewGuid()),
            PipelineTestData.Question(questionId: 202, linkId: Guid.NewGuid())
        };

        var result = await sut.ProcessAsync(batch);

        Assert.Single(result.FoundQuestions);
        Assert.Single(result.BannedQuestions);
        Assert.Equal(101, result.FoundQuestions[0].Id);
        Assert.Equal(202, result.BannedQuestions[0].Id);
    }
}
