using LinkTracker.Bot.Core.HandlerTypes;
using LinkTracker.Bot.Core.HandlerTypes.Attributes;
using LinkTracker.Bot.Core.StateMachine;
using LinkTracker.Bot.Core.StateMachine.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace LinkTracker.Bot.App.Features.Bot.GlobalCancel;

[BotCommand("cancel", "Отменяет выполнение действия")]
public class GlobalCancelCommand(ITelegramBotClient botClient, IStateDataStore dataStore, UserSession userSession) : IMessageHandler
{
    public async Task HandleAsync(Message msg, CancellationToken ct)
    {
        userSession.ResetState();
        dataStore.ResetData(userSession.ChatId);

        await botClient.SendMessage(userSession.ChatId, "Действие отменено", cancellationToken: ct);
    }
}