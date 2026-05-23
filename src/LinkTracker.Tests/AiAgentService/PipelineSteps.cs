using System.Net;
using AiAgentService.Web.Configurations;
using AiAgentService.Web.Infrastructure.ExternalApi;
using AiAgentService.Web.Pipeline.Data;
using AiAgentService.Web.Pipeline.Filters;
using AiAgentService.Web.Pipeline.Filters.BannedAuthors;
using AiAgentService.Web.Pipeline.Filters.LengthFilter;
using AiAgentService.Web.Pipeline.Filters.StopWords;
using AiAgentService.Web.Pipeline.Handlers.Summarization;
using Confluent.Kafka;
using LinkTracker.Scrapper.Contracts.Dtos.Notifications;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Refit;

namespace LinkTracker.Tests.AiAgentService;

public class PipelineSteps
{
    private AggregateFilter SetupFilters(FiltersConfiguration config)
    {
        var options = Options.Create(config);
        var wordsFilter = new StopWordsFilter(options, NullLogger<StopWordsFilter>.Instance);
        var authorsFilter = new BannedAuthorsFilter(options, NullLogger<BannedAuthorsFilter>.Instance);
        var lenFilter = new LengthFilter(options, NullLogger<LengthFilter>.Instance);

        return new AggregateFilter([wordsFilter, authorsFilter, lenFilter]);
    }

    private TopicPartitionOffset GetRandomTopicPartitionOffset()
    {
        return new TopicPartitionOffset(new TopicPartition("asdf", new Partition(1)), new Offset(0L));
    }

    [Fact]
    public void Test_MessageContainsBanWord()
    {
        var config = new FiltersConfiguration();
        config.StopWords = ["novichok"];
        var filters = SetupFilters(config);
        var msg = new UpdateMessage(Guid.NewGuid(), "https://cool-site.net", "Yanis varufakis", "novichok podskazhet", []);
        var wrap = new MessageWrap(msg, GetRandomTopicPartitionOffset(), null, false);

        var result = filters.Handle(wrap);
        Assert.True(result.IsFiltered);
    }

    [Fact]
    public void Test_MessageContainsBannedAuthor()
    {
        var config = new FiltersConfiguration();
        config.ExcludedAuthors = ["Mista"];
        var filters = SetupFilters(config);
        var msg = new UpdateMessage(Guid.NewGuid(), "https://cool-site.net", "Mista", "some cool content", []);
        var wrap = new MessageWrap(msg, GetRandomTopicPartitionOffset(), null, false);

        var result = filters.Handle(wrap);
        Assert.True(result.IsFiltered);
    }

    [Fact]
    public void Test_MessageLengthLowerThanMinLength()
    {
        var config = new FiltersConfiguration();
        config.MinLength = 5;
        var filters = SetupFilters(config);
        var msg = new UpdateMessage(Guid.NewGuid(), "https://cool-site.net", "Mista", "a", []);
        var wrap = new MessageWrap(msg, GetRandomTopicPartitionOffset(), null, false);

        var result = filters.Handle(wrap);
        Assert.True(result.IsFiltered);
    }

    [Fact]
    public void Test_CorrectMessage()
    {
        var config = new FiltersConfiguration();
        config.MinLength = 5;
        config.ExcludedAuthors = ["Mister Twister"];
        config.StopWords = ["X5 93 year", "карбюратор", "инжектор", "инспектор", "проректор"];
        var filters = SetupFilters(config);
        var msg = new UpdateMessage(Guid.NewGuid(), "https://prodam.net", "MrFelixm", "ne prodam", []);
        var wrap = new MessageWrap(msg, GetRandomTopicPartitionOffset(), null, false);

        var result = filters.Handle(wrap);
        Assert.False(result.IsFiltered);
    }

    [Fact]
    public async Task Test_DontSummarizeShortText()
    {
        var config = new FiltersConfiguration();
        config.Summarization.Threshold = 5;
        var options = Options.Create(config);
        var client = Substitute.For<IOpenAiApiClient>();

        client.CreateChatCompletionAsync(Arg.Any<ChatCompletionRequest>()).Returns(
            new ApiResponse<ChatCompletionResponse>(new HttpResponseMessage(), null, new RefitSettings()
            ));

        var summarizationHandler = new SummarizationHandler(options, client, NullLogger<SummarizationHandler>.Instance);

        var msg = new UpdateMessage(Guid.NewGuid(), "https://prodam.net", "MrFelixm", "текс", []);
        var wrap = new MessageWrap(msg, GetRandomTopicPartitionOffset(), null, false);

        var res = await summarizationHandler.HandleAsync(wrap);
        Assert.Equal(res.Message.Description, msg.Description);
    }

    [Fact]
    public async Task Test_SummarizeLongText()
    {
        var config = new FiltersConfiguration();
        config.Summarization.Threshold = 5;
        var options = Options.Create(config);
        var client = Substitute.For<IOpenAiApiClient>();

        client.CreateChatCompletionAsync(Arg.Any<ChatCompletionRequest>()).Returns(
            new ApiResponse<ChatCompletionResponse>(new HttpResponseMessage(statusCode: HttpStatusCode.OK),
                new ChatCompletionResponse() { Choices = [new ChatChoice() { Message = new ChatMessage() { Content = "summarized text" } }] },
                new RefitSettings()
            ));
        var summarizationHandler = new SummarizationHandler(options, client, NullLogger<SummarizationHandler>.Instance);

        var msg = new UpdateMessage(Guid.NewGuid(), "https://prodam.net", "MrFelixm", "длииннныыый текст", []);
        var wrap = new MessageWrap(msg, GetRandomTopicPartitionOffset(), null, false);

        var res = await summarizationHandler.HandleAsync(wrap);
        Assert.NotEqual(res.Message.Description, msg.Description);
        Assert.NotEqual("summarized text", res.Message.Description);
    }
}