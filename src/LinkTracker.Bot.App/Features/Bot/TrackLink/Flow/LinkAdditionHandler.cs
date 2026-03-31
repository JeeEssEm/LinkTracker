using System.Net;
using FluentResults;
using LinkTracker.Bot.App.ApiClients;
using LinkTracker.Bot.App.ApiClients.Scrapper;
using LinkTracker.Bot.App.ApiClients.Scrapper.Dtos;
using LinkTracker.Bot.App.ApiClients.Scrapper.Errors.Chats;
using LinkTracker.Bot.App.ApiClients.Scrapper.Errors.Links;
using LinkTracker.Bot.App.ApiClients.Scrapper.Rest;
using LinkTracker.Bot.App.Domain;
using LinkTracker.Bot.Core.StateMachine;
using LinkTracker.Bot.Core.StateMachine.Abstractions;
using Telegram.Bot;

namespace LinkTracker.Bot.App.Features.Bot.TrackLink.Flow;

public class LinkAdditionHandler(
    IStateDataStore dataStore,
    IScrapperClient scrapperClient,
    ITelegramBotClient botClient,
    UserSession userSession)
{
    public async Task HandleAddLink(List<string> tags, CancellationToken ct)
    {
        var chatId = userSession.ChatId;
        var linkRecord = dataStore.GetData<LinkRecord>(chatId);

        var response = await scrapperClient.AddLinkAsync(
            new AddLinkDto(linkRecord.Link, tags, []),
            chatId);
        var message = "Ссылка успешно добавлена! Список ссылок /links";

        if (response.IsFailed)
        {
            message = MapErrorFromResult(response);
        }

        await botClient.SendMessage(chatId, message, cancellationToken: ct);

        userSession.ResetState();
        dataStore.ResetData(chatId);
    }

    private string MapErrorFromResult(Result result)
    {
        return result.Errors.First() switch
        {
            LinkAlreadyExists => "Ссылка уже существует! Список ссылок /links",
            UnsupportedLinkType unsupportedLinkType => "Данный тип ссылки не поддерживается. Поддерживаются только ссылки вида:\n" +
                                                       string.Join("\n-", unsupportedLinkType.SupportedTypes),
            ChatNotFound => "Вы не зарегистрированы! Напишите /start",
            _ => "Произошла непредвиденная ошибка"
        };
    }
}