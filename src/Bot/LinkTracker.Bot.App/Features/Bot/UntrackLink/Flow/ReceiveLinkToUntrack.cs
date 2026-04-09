using System.Diagnostics;
using System.Net;
using LinkTracker.Bot.App.ApiClients;
using LinkTracker.Bot.App.ApiClients.Scrapper;
using LinkTracker.Bot.App.ApiClients.Scrapper.Rest;
using LinkTracker.Bot.Core.HandlerTypes;
using LinkTracker.Bot.Core.HandlerTypes.Attributes;
using LinkTracker.Bot.Core.StateMachine;
using LinkTracker.Bot.Core.StateMachine.Attributes;
using LinkTracker.Scrapper.Contracts.Dtos.Links;
using LinkTracker.Scrapper.Errors.Chats;
using LinkTracker.Scrapper.Errors.Links;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace LinkTracker.Bot.App.Features.Bot.UntrackLink.Flow;

[UpdateHandler(UpdateType.Message)]
[RequireState<UntrackLinkStates>(UntrackLinkStates.WaitingLink)]
public class ReceiveLinkToUntrack(IScrapperClient scrapperClient, UserSession userSession, ITelegramBotClient botClient) : IUpdateHandler
{
    public async Task HandleAsync(Update update, CancellationToken ct)
    {
        userSession.ResetState();
        var res = await scrapperClient.RemoveLinkAsync(new RemoveLinkDto(update.Message.Text), userSession.ChatId);
        var message = "Ссылка больше не отслеживается. Список всех ссылок /links";
        if (res.IsFailed)
        {
            var error = res.Errors.First();
            message = error switch
            {
                LinkNotFound => "Ссылка для удаления не найдена",
                ChatNotFound => "Вы не зарегистрированы! Напишите /start",
                _ => throw new InvalidOperationException()
            };
        }

        await botClient.SendMessage(userSession.ChatId, message, cancellationToken: ct);
    }
}