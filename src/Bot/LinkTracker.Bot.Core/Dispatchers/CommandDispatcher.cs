using LinkTracker.Bot.Core.HandlerTypes;
using LinkTracker.Bot.Core.Registries;
using LinkTracker.Bot.Core.Registries.Descriptors;
using LinkTracker.Bot.Core.StateMachine;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace LinkTracker.Bot.Core.Dispatchers;

public class CommandDispatcher(
    CommandRegistry commandRegistry,
    IServiceProvider serviceProvider,
    ITelegramBotClient bot,
    ILogger<CommandDispatcher> logger,
    UserSession userSession,
    Action<CommandDescriptor?>? actionAfterDispatch = null
    )
{
    public async Task DispatchAsync(Message message, CancellationToken ct)
    {
        var descriptor = commandRegistry.SearchCommand(message.Text!, userSession.State);
        var fallbackDescriptor = commandRegistry.SearchCommand(message.Text!, null);

        if (userSession.State != null && descriptor == null)
        {
            userSession.ResetState();
        }
        if (descriptor == null && fallbackDescriptor == null)
        {
            logger.LogInformation("Unknown command was used. MessageText={cmd}", message.Text);
            await bot.SendMessage(
                message.Chat,
                "Неизвестная команда. Используйте /help для получения списка всех доступных команд",
                cancellationToken: ct);
            return;
        }

        IMessageHandler cmd;
        if (descriptor != null)
        {
            cmd = (IMessageHandler)serviceProvider.GetRequiredService(descriptor.CommandType);
        }
        else
        {
            cmd = (IMessageHandler)serviceProvider.GetRequiredService(fallbackDescriptor.CommandType);
        }
        await cmd.HandleAsync(message, ct);
        actionAfterDispatch?.Invoke(descriptor);
    }
}