using LinkTracker.Bot.App.Features.Bot.TrackLink.Flow;
using LinkTracker.Bot.Core.HandlerTypes;
using LinkTracker.Bot.Core.HandlerTypes.Attributes;
using LinkTracker.Bot.Core.StateMachine;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace LinkTracker.Bot.App.Features.Bot.TrackLink;

[BotCommand("track", "Добавить ссылку для отслеживания")]
public class TrackLinkCommand(UserSession session, ITelegramBotClient botClient) : IMessageHandler
{
    public async Task HandleAsync(Message msg, CancellationToken ct)
    {
        await botClient.SendMessage(session.ChatId, "Введите ссылку для отслеживания", cancellationToken: ct);
        session.SetState(TrackLinkStates.WaitingForLink);
    }
}
