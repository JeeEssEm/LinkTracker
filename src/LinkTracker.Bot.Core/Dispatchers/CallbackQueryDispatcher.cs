using LinkTracker.Bot.Core.HandlerTypes;
using LinkTracker.Bot.Core.Registries;
using LinkTracker.Bot.Core.StateMachine;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace LinkTracker.Bot.Core.Dispatchers;

public class CallbackQueryDispatcher(CallbackQueryRegistry callbackQueryRegistry, UserSession userSession, IServiceProvider serviceProvider, ITelegramBotClient bot, ILogger<CallbackQueryDispatcher> logger)
{
    public async Task DispatchAsync(CallbackQuery callbackQuery, CancellationToken ct)
    {
        var descriptor = callbackQueryRegistry.SearchCallbackQuery(callbackQuery, userSession.State);
        if (descriptor is null)
        {
            await bot.SendMessage(callbackQuery.From.Id, "Извините, команда не была исполнена", cancellationToken: ct);
            logger.LogWarning("CallbackQuery with pattern={pattern} not found", callbackQuery.Data);
            return;
        }

        var cbHandler = (ICallbackQuery)serviceProvider.GetRequiredService(descriptor.CallbackQueryType);
        await cbHandler.HandleAsync(callbackQuery, ct);
    }
}