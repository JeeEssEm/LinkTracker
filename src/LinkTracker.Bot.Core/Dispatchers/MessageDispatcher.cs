using LinkTracker.Bot.Core.HandlerTypes;
using LinkTracker.Bot.Core.Registries;
using LinkTracker.Bot.Core.StateMachine;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace LinkTracker.Bot.Core.Dispatchers;

public class MessageDispatcher(ITelegramBotClient botClient, MessageHandlersRegistry messageHandlersRegistry, UserSession userSession, IServiceProvider serviceProvider)
{
    public async Task DispatchAsync(Update update, CancellationToken ct)
    {
        if (userSession.State == null)
        {
            await botClient.SendMessage(userSession.ChatId, "Извините, мы не можем обработать ваш запрос", cancellationToken: ct);
            return;
        }

        var descriptor = messageHandlersRegistry.SearchHandler(userSession.State);
        if (descriptor == null)
        {
            await botClient.SendMessage(userSession.ChatId, "Извините, мы не можем обработать ваш запрос", cancellationToken: ct);
            return;
        }

        var handler = (IUpdateHandler)serviceProvider.GetRequiredService(descriptor.HandlerType);
        await handler.HandleAsync(update, ct);
    }
}