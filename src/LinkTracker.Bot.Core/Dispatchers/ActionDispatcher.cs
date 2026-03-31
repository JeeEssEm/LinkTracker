using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace LinkTracker.Bot.Core.Dispatchers;


public class ActionDispatcher(CommandDispatcher commandDispatcher, CallbackQueryDispatcher callbackQueryDispatcher, MessageDispatcher messageDispatcher, ILogger<ActionDispatcher> logger)
{
    public async Task DispatchAsync(Update update, CancellationToken ct)
    {
        switch (update.Type)
        {
            case UpdateType.Message:
                if (update.Message!.Text.StartsWith("/"))
                {
                    await commandDispatcher.DispatchAsync(update.Message, ct);
                }
                else
                {
                    await messageDispatcher.DispatchAsync(update, ct);
                }
                break;
            case UpdateType.CallbackQuery:
                await callbackQueryDispatcher.DispatchAsync(update.CallbackQuery!, ct);
                break;
            default:
                await messageDispatcher.DispatchAsync(update, ct);
                break;
        }
    }
}