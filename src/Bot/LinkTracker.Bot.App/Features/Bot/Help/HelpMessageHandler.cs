using LinkTracker.Bot.Core.HandlerTypes;
using LinkTracker.Bot.Core.HandlerTypes.Attributes;
using LinkTracker.Bot.Core.Registries;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace LinkTracker.Bot.App.Features.Bot.Help;

[BotCommand("help", "Выводит все доступные команды")]
public class HelpMessageHandler(ITelegramBotClient bot, CommandRegistry commandRegistry) : IMessageHandler
{
    public async Task HandleAsync(Message msg, CancellationToken ct)
    {
        var descriptions = commandRegistry.GetCommandDescriptionsList();
        var resultText = $"<b>Все доступные команды:</b>\n{string.Join('\n', descriptions)}";

        await bot.SendMessage(msg.Chat, resultText, cancellationToken: ct, parseMode: ParseMode.Html);
    }
}