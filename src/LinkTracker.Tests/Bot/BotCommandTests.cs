using System.Net;
using FluentResults;
using LinkTracker.Bot.App.ApiClients;
using LinkTracker.Bot.App.ApiClients.Scrapper;
using LinkTracker.Bot.App.ApiClients.Scrapper.Rest;
using LinkTracker.Bot.App.Domain;
using LinkTracker.Bot.App.Features.Bot.TrackLink.Flow;
using LinkTracker.Bot.App.StateController;
using LinkTracker.Bot.Core;
using LinkTracker.Bot.Core.DependencyInjectionExtensions;
using LinkTracker.Bot.Core.Dispatchers;
using LinkTracker.Bot.Core.StateMachine;
using LinkTracker.Bot.Core.StateMachine.Abstractions;
using LinkTracker.Scrapper.Contracts.Dtos.Links;
using LinkTracker.Scrapper.Errors.Chats;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Refit;

namespace LinkTracker.Tests.Bot;

public class BotCommandTests
{
    private readonly ITelegramBotClient _botMock;
    private readonly ServiceProvider _serviceProvider;
    private readonly IScrapperClient _scrapperClient;

    public BotCommandTests()
    {
        _botMock = Substitute.For<ITelegramBotClient>();
        _scrapperClient = Substitute.For<IScrapperClient>();
        var services = new ServiceCollection();

        services.AddSingleton(_botMock);
        services.AddSingleton(_scrapperClient);
        services.AddScoped<LinkAdditionHandler>();
        services.AddSingleton<IStateController, InMemoryStateController>();
        services.AddTelegramBotDependencies();
        services.AddLogging();

        _serviceProvider = services.BuildServiceProvider();
    }

    private async Task DispatchAsync(string text, long chatId)
    {
        using var scope = _serviceProvider.CreateScope();
        var session = scope.ServiceProvider.GetRequiredService<UserSession>();
        session.ChatId = chatId;

        var stateController = scope.ServiceProvider.GetRequiredService<IStateController>();
        var savedState = await stateController.GetStateAsync(chatId);
        session.SetState(savedState);

        var dispatcher = scope.ServiceProvider.GetRequiredService<ActionDispatcher>();
        var update = CreateMessage(text, chatId);
        await dispatcher.DispatchAsync(update, CancellationToken.None);

        if (session.State != savedState)
        {
            await stateController.SetStateAsync(chatId, session.State);
        }
    }

    private static Update CreateMessage(string text, long chatId)
    {
        return new Update
        {
            Message = new Message { Text = text, Chat = new Chat { Id = chatId }, Id = 1 }
        };
    }

    [Fact]
    public async Task StartCommand_SendsWelcomeMessage()
    {
        _scrapperClient.AddTgChatAsync(Arg.Any<long>()).Returns(_ => new Result());
        await DispatchAsync("/start", 1);

        await _botMock.Received(1).SendRequest(
            Arg.Is<SendMessageRequest>(r => r.ChatId == 1 && r.Text.Contains("Добро пожаловать")),
            Arg.Any<CancellationToken>());
        await _scrapperClient.Received(1).AddTgChatAsync(Arg.Is<long>(a => a == 1));
    }

    [Fact]
    public async Task HelpCommand_SendsCommandList()
    {
        await DispatchAsync("/help", 2);

        await _botMock.Received(1).SendRequest(
            Arg.Is<SendMessageRequest>(r => r.ChatId == 2 && r.Text.Contains("доступные команды")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UnknownCommand_SendsError()
    {
        await DispatchAsync("/nonexistent", 3);

        await _botMock.Received(1).SendRequest(
            Arg.Is<SendMessageRequest>(r => r.ChatId == 3 && r.Text.Contains("Неизвестная команда")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UnknownMessage_SendsCannotProcess()
    {
        await DispatchAsync("random text", 4);

        await _botMock.Received(1).SendRequest(
            Arg.Is<SendMessageRequest>(r => r.ChatId == 4 && r.Text.Contains("не можем обработать")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LinksCommand_NoLinks_SendsEmptyMessage()
    {
        var chatId = 5;
        _scrapperClient.ListLinksAsync(chatId)
            .Returns(Task.FromResult(Result.Ok(new ListLinksResponse(new List<LinkResponse>(), 0))));

        await DispatchAsync("/links", chatId);

        await _botMock.Received(1).SendRequest(
            Arg.Is<SendMessageRequest>(r => r.ChatId == chatId && r.Text.Contains("нет ссылок")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LinksCommand_WithLinks_SendsList()
    {
        var chatId = 6;
        var links = new List<LinkResponse>
        {
            new(1, "https://github.com/test/repo", new List<string> { "work" })
        };
        _scrapperClient.ListLinksAsync(chatId)
            .Returns(Task.FromResult(Result.Ok(new ListLinksResponse(links, links.Count))));

        await DispatchAsync("/links", chatId);

        await _botMock.Received(1).SendRequest(
            Arg.Is<SendMessageRequest>(r =>
                r.ChatId == chatId
                && r.Text.Contains("https://github.com/test/repo")
                && r.Text.Contains("work")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LinksCommand_WithTagFilter_FiltersLinks()
    {
        var chatId = 7;
        var links = new List<LinkResponse>
        {
            new (1, "https://github.com/a/b", new List<string> { "work" }),
            new (2, "https://github.com/c/d", new List<string> { "hobby" })
        };
        _scrapperClient.ListLinksAsync(chatId)
            .Returns(Result.Ok(new ListLinksResponse(links, links.Count)));

        await DispatchAsync("/links work", chatId);

        await _botMock.Received(1).SendRequest(
            Arg.Is<SendMessageRequest>(r =>
                r.ChatId == chatId
                && r.Text.Contains("https://github.com/a/b")
                && !r.Text.Contains("https://github.com/c/d")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TrackCommand_AsksForLink()
    {
        await DispatchAsync("/track", 8);

        await _botMock.Received(1).SendRequest(
            Arg.Is<SendMessageRequest>(r => r.ChatId == 8 && r.Text.Contains("ссылку")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TrackFlow_InvalidLink_AsksAgain()
    {
        await DispatchAsync("/track", 9);
        await DispatchAsync("not-a-url", 9);

        await _botMock.Received().SendRequest(
            Arg.Is<SendMessageRequest>(r => r.ChatId == 9 && r.Text.Contains("не найдена")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelCommand_SendsConfirmation()
    {
        await DispatchAsync("/track", 10);
        await DispatchAsync("/cancel", 10);

        await _botMock.Received().SendRequest(
            Arg.Is<SendMessageRequest>(r => r.ChatId == 10 && r.Text.Contains("отменено")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UntrackCommand_AsksForLink()
    {
        await DispatchAsync("/untrack", 11);

        await _botMock.Received(1).SendRequest(
            Arg.Is<SendMessageRequest>(r => r.ChatId == 11 && r.Text.Contains("ссылку")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TrackFlow_DuplicateLink_NotifiesUser()
    {
        var chatId = 12;
        var url = "https://github.com/dup/repo";

        _scrapperClient.AddLinkAsync(Arg.Is<AddLinkDto>(d => d.Link == url), chatId)
            .Returns(Task.FromResult(Result.Fail(new ChatNotFound())));

        await DispatchAsync("/track", chatId);

        await DispatchAsync(url, chatId);
        await DispatchAsync("some tags", chatId);

        await _botMock.Received().SendRequest(
            Arg.Is<SendMessageRequest>(r =>
                r.ChatId == chatId ||
                r.Text.Contains("уже отслеживается") ||
                r.Text.Contains("введите")
                ),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TrackFlow_ValidLinkAndTags_SavesData()
    {
        var chatId = 13;
        var url = "https://github.com/user/repo";
        var tags = "work, hobby";

        _scrapperClient.AddLinkAsync(Arg.Any<AddLinkDto>(), chatId)
            .Returns(Task.FromResult(Result.Ok()));

        await DispatchAsync("/track", chatId);
        await DispatchAsync(url, chatId);
        await DispatchAsync(tags, chatId);

        await _scrapperClient.Received(1).AddLinkAsync(
            Arg.Is<AddLinkDto>(d => d.Link == url && d.Tags.SequenceEqual(new List<string> { "work", "hobby" })),
            chatId);

        await _botMock.Received().SendRequest(
            Arg.Is<SendMessageRequest>(r => r.ChatId == chatId && r.Text.Contains("успешно добавлена")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TrackFlow_InvalidSchemeUrl_NotifiesUser()
    {
        await DispatchAsync("/track", 14);
        await DispatchAsync("tbank://github.com/user/repo", 14);

        await _botMock.Received().SendRequest(
            Arg.Is<SendMessageRequest>(r => r.ChatId == 14 && r.Text.Contains("не найдена")),
            Arg.Any<CancellationToken>());
    }
}