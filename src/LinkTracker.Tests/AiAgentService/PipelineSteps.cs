using System.Net;
using AiAgentService.Web.Configurations;
using AiAgentService.Web.Infrastructure;
using AiAgentService.Web.Infrastructure.ExternalApi;
using AiAgentService.Web.Pipeline;
using AiAgentService.Web.Pipeline.Data;
using AiAgentService.Web.Pipeline.Filters;
using AiAgentService.Web.Pipeline.Filters.BannedAuthors;
using AiAgentService.Web.Pipeline.Filters.LengthFilter;
using AiAgentService.Web.Pipeline.Filters.StopWords;
using AiAgentService.Web.Pipeline.Handlers.Kafka;
using AiAgentService.Web.Pipeline.Handlers.Prioritizing;
using AiAgentService.Web.Pipeline.Handlers.Summarization;
using Confluent.Kafka;
using LinkTracker.Bot.Contracts.Rest;
using LinkTracker.Scrapper.Contracts.Dtos.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using NSubstitute;
using Refit;

namespace LinkTracker.Tests.AiAgentService;

public class PipelineSteps
{
    private readonly Random _rnd = new();

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
        return new TopicPartitionOffset(new TopicPartition("asdf", new Partition(1)), new Offset(_rnd.NextInt64(0, 1_000_000_000)));
    }

    [Fact]
    public void Test_MessageContainsBanWord()
    {
        var config = new FiltersConfiguration();
        config.StopWords = ["novichok"];
        var filters = SetupFilters(config);
        var msg = new UpdateMessage(Guid.NewGuid(), "https://cool-site.net", "Yanis varufakis", "novichok podskazhet", []);
        var wrap = new MessageData(msg, GetRandomTopicPartitionOffset(), MessagePriority.Low, false);

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
        var wrap = new MessageData(msg, GetRandomTopicPartitionOffset(), MessagePriority.Low, false);

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
        var wrap = new MessageData(msg, GetRandomTopicPartitionOffset(), MessagePriority.Low, false);

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
        var wrap = new MessageData(msg, GetRandomTopicPartitionOffset(), MessagePriority.Low, false);

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
        var wrap = new MessageData(msg, GetRandomTopicPartitionOffset(), MessagePriority.Low, false);

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
        var wrap = new MessageData(msg, GetRandomTopicPartitionOffset(), MessagePriority.Low, false);

        var res = await summarizationHandler.HandleAsync(wrap);
        Assert.NotEqual(res.Message.Description, msg.Description);
        Assert.NotEqual("summarized text", res.Message.Description);
    }

    [Fact]
    public async Task Test_PrioritizeHandler_CorrectlyIdentifiesHighPriority()
    {
        var client = Substitute.For<IOpenAiApiClient>();
        client.CreateChatCompletionAsync(Arg.Any<ChatCompletionRequest>()).Returns(
            new ApiResponse<ChatCompletionResponse>(new HttpResponseMessage(statusCode: HttpStatusCode.OK),
                new ChatCompletionResponse() { Choices = [new ChatChoice() { Message = new ChatMessage() { Content = "summarized text" } }] },
                new RefitSettings()
            ));
        var options = Options.Create(new PrioritizationConfiguration()
        {
            HighKeywords = ["critical", "urgent", "breaking", "security"],
            LowKeywords = ["minor", "typo", "chore", "docs"]
        });
        var handler = new PrioritizingHandler(options, client, NullLogger<PrioritizingHandler>.Instance);

        var result = await handler.HandleAsync(
            new MessageData(
                new UpdateMessage(Guid.NewGuid(), "", "", "critical bug fix", []),
                GetRandomTopicPartitionOffset()
        ));
        Assert.Equal(MessagePriority.High, result.Priority);
    }

    [Fact]
    public async Task Test_PrioritizeHandler_CorrectlyIdentifiesMediumPriority()
    {
        var client = Substitute.For<IOpenAiApiClient>();
        client.CreateChatCompletionAsync(Arg.Any<ChatCompletionRequest>()).Returns(
            new ApiResponse<ChatCompletionResponse>(new HttpResponseMessage(statusCode: HttpStatusCode.OK),
                new ChatCompletionResponse() { Choices = [new ChatChoice() { Message = new ChatMessage() { Content = "summarized text" } }] },
                new RefitSettings()
            ));
        var options = Options.Create(new PrioritizationConfiguration()
        {
            HighKeywords = ["critical", "urgent", "breaking", "security"],
            LowKeywords = ["minor", "typo", "chore", "docs"]
        });
        var handler = new PrioritizingHandler(options, client, NullLogger<PrioritizingHandler>.Instance);

        var result = await handler.HandleAsync(
            new MessageData(
                new UpdateMessage(Guid.NewGuid(), "", "", "fix something in codebase", []),
                GetRandomTopicPartitionOffset()
            ));
        Assert.Equal(MessagePriority.Medium, result.Priority);
    }

    [Fact]
    public async Task Test_PrioritizeHandler_CorrectlyIdentifiesLowPriority()
    {
        var client = Substitute.For<IOpenAiApiClient>();
        client.CreateChatCompletionAsync(Arg.Any<ChatCompletionRequest>()).Returns(
            new ApiResponse<ChatCompletionResponse>(new HttpResponseMessage(statusCode: HttpStatusCode.OK),
                new ChatCompletionResponse() { Choices = [new ChatChoice() { Message = new ChatMessage() { Content = "summarized text" } }] },
                new RefitSettings()
            ));
        var options = Options.Create(new PrioritizationConfiguration()
        {
            HighKeywords = ["critical", "urgent", "breaking", "security"],
            LowKeywords = ["minor", "typo", "chore", "docs"]
        });
        var handler = new PrioritizingHandler(options, client, NullLogger<PrioritizingHandler>.Instance);

        var result = await handler.HandleAsync(
            new MessageData(
                new UpdateMessage(Guid.NewGuid(), "", "", "fix typo in readme", []),
                GetRandomTopicPartitionOffset()
            ));
        Assert.Equal(MessagePriority.Low, result.Priority);
    }

    [Fact]
    public async Task Test_GroupingConcatenatesUpdatesForSameChatId()
    {
        var services = new ServiceCollection();
        var consumer = Substitute.For<IConsumerUpdatesManager>();
        var kafkaHandler = Substitute.For<ISendToKafkaHandler>();
        var client = Substitute.For<IOpenAiApiClient>();
        client.CreateChatCompletionAsync(Arg.Any<ChatCompletionRequest>()).Returns(
            new ApiResponse<ChatCompletionResponse>(new HttpResponseMessage(statusCode: HttpStatusCode.BadRequest),
                new ChatCompletionResponse(),
                new RefitSettings()
            ));

        services.AddSingleton<AggregateFilter>();
        services.AddLogging(builder => builder.AddProvider(NullLoggerProvider.Instance));

        services.AddScoped<IOpenAiApiClient>(_ => client);
        services.AddScoped<SummarizationHandler>();
        services.AddScoped<PrioritizingHandler>();
        services.AddSingleton<MessageProcessingPipeline>();
        services.AddSingleton<IConsumerUpdatesManager>(_ => consumer);
        services.AddSingleton<ISendToKafkaHandler>(_ => kafkaHandler);
        services.AddSingleton<IOptions<FiltersConfiguration>>(_ => Options.Create(new FiltersConfiguration() { MinLength = 1 }));
        services.AddSingleton<IOptions<PrioritizationConfiguration>>(_ => Options.Create(new PrioritizationConfiguration() { }));
        services.AddSingleton<IOptions<GroupingConfiguration>>(_ => Options.Create(new GroupingConfiguration() { WindowMs = 2000 }));

        var serviceProvider = services.BuildServiceProvider();
        var pipeline = serviceProvider.GetRequiredService<MessageProcessingPipeline>();

        var msg1 = new UpdateMessage(Guid.NewGuid(), "https://good_url1.com", "hz", "first description", [1L]);
        var msg2 = new UpdateMessage(Guid.NewGuid(), "https://good_url2.com", "hz2", "second description", [1L, 2L]);
        var offset1 = GetRandomTopicPartitionOffset();
        var offset2 = GetRandomTopicPartitionOffset();

        await pipeline.PushMessage(new MessageData(msg1, offset1));
        await pipeline.PushMessage(new MessageData(msg2, offset2));

        await pipeline.Complete();

        await Task.Delay(4000);

        await kafkaHandler.Received(1).Handle(Arg.Is<object>(a =>
            (a as LinkUpdateRequest).TgChatIds.SequenceEqual(new[] { 1L }) &&
            (a as LinkUpdateRequest).Description.Contains("1. first description") &&
            (a as LinkUpdateRequest).Description.Contains("2. second description") &&
            (a as LinkUpdateRequest).Priority == "High"
            ));
        await kafkaHandler.Received(1).Handle(Arg.Is<object>(a =>
            (a as LinkUpdateRequest).TgChatIds.SequenceEqual(new[] { 2L }) &&
            (a as LinkUpdateRequest).Description == "second description" &&
            (a as LinkUpdateRequest).Priority == "Medium"
            ));
    }

    [Fact]
    public async Task Test_GroupingDoesNotConcatenatesUpdatesForSameChatId()
    {
        var services = new ServiceCollection();
        var consumer = Substitute.For<IConsumerUpdatesManager>();
        var kafkaHandler = Substitute.For<ISendToKafkaHandler>();
        var client = Substitute.For<IOpenAiApiClient>();
        client.CreateChatCompletionAsync(Arg.Any<ChatCompletionRequest>()).Returns(
            new ApiResponse<ChatCompletionResponse>(new HttpResponseMessage(statusCode: HttpStatusCode.BadRequest),
                new ChatCompletionResponse(),
                new RefitSettings()
            ));

        services.AddSingleton<AggregateFilter>();
        services.AddLogging(builder => builder.AddProvider(NullLoggerProvider.Instance));

        services.AddScoped<IOpenAiApiClient>(_ => client);
        services.AddScoped<SummarizationHandler>();
        services.AddScoped<PrioritizingHandler>();
        services.AddSingleton<MessageProcessingPipeline>();
        services.AddSingleton<IConsumerUpdatesManager>(_ => consumer);
        services.AddSingleton<ISendToKafkaHandler>(_ => kafkaHandler);
        services.AddSingleton<IOptions<FiltersConfiguration>>(_ => Options.Create(new FiltersConfiguration() { MinLength = 1 }));
        services.AddSingleton<IOptions<PrioritizationConfiguration>>(_ => Options.Create(new PrioritizationConfiguration() { }));
        services.AddSingleton<IOptions<GroupingConfiguration>>(_ => Options.Create(new GroupingConfiguration() { WindowMs = 2000 }));

        var serviceProvider = services.BuildServiceProvider();
        var pipeline = serviceProvider.GetRequiredService<MessageProcessingPipeline>();

        var msg1 = new UpdateMessage(Guid.NewGuid(), "https://good_url1.com", "hz", "first description", [1L]);
        var offset1 = GetRandomTopicPartitionOffset();

        await pipeline.PushMessage(new MessageData(msg1, offset1));

        await pipeline.Complete();

        await Task.Delay(4000);

        await kafkaHandler.Received(1).Handle(Arg.Is<object>(a =>
            (a as LinkUpdateRequest).TgChatIds.SequenceEqual(new[] { 1L }) &&
            (a as LinkUpdateRequest).Description == "first description" &&
            (a as LinkUpdateRequest).Priority == "Medium"
            ));
    }
}