using LinkTracker.Bot.App.ApiClients;
using LinkTracker.Bot.App.ApiClients.Scrapper.Rest;
using LinkTracker.Bot.App.Domain;
using LinkTracker.Bot.Core.HandlerTypes;
using LinkTracker.Bot.Core.HandlerTypes.Attributes;
using LinkTracker.Bot.Core.StateMachine;
using LinkTracker.Bot.Core.StateMachine.Abstractions;
using LinkTracker.Bot.Core.StateMachine.Attributes;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace LinkTracker.Bot.App.Features.Bot.TrackLink.Flow;

[UpdateHandler(UpdateType.Message)]
[RequireState<TrackLinkStates>(TrackLinkStates.WaitingForLink)]
public class ReceiveLink(UserSession userSession, ITelegramBotClient botClient, IStateDataStore dataStore) : IUpdateHandler
{
    public async Task HandleAsync(Update update, CancellationToken ct)
    {
        var link = update.Message!.Text;
        if (Uri.TryCreate(link, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
        {
            dataStore.SetData(userSession.ChatId, new LinkRecord(link, []));

            await botClient.SendMessage(userSession.ChatId, $"Теперь введите теги через запятую", replyMarkup: CreateSkipButtonMarkup(), cancellationToken: ct);

            userSession.SetState(TrackLinkStates.WaitingForTags);
            return;
        }

        await botClient.SendMessage(
            userSession.ChatId,
            "Ссылка в сообщении не найдена или имеет некорректный формат. Попробуйте еще раз",
            cancellationToken: ct
        );
    }

    private InlineKeyboardMarkup CreateSkipButtonMarkup()
    {
        return new InlineKeyboardMarkup(new InlineKeyboardButton("Пропустить ввод тегов", "skip_tags"));
    }
}