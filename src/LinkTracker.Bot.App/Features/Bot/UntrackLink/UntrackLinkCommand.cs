using LinkTracker.Bot.App.Features.Bot.UntrackLink.Flow;
using LinkTracker.Bot.Core.HandlerTypes;
using LinkTracker.Bot.Core.HandlerTypes.Attributes;
using LinkTracker.Bot.Core.StateMachine;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace LinkTracker.Bot.App.Features.Bot.UntrackLink;

[BotCommand("untrack", "отмена отслеживания ссылки")]
public class UntrackLinkCommand(ITelegramBotClient botClient, UserSession userSession) : IMessageHandler
{
    public async Task HandleAsync(Message msg, CancellationToken ct)
    {
        await botClient.SendMessage(msg.Chat.Id, "Введите ссылку для удаления", cancellationToken: ct);
        userSession.SetState(UntrackLinkStates.WaitingLink);
    }
}