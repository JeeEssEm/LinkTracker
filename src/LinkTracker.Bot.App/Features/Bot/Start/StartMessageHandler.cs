using System.Net;
using LinkTracker.Bot.App.ApiClients;
using LinkTracker.Bot.App.ApiClients.Scrapper;
using LinkTracker.Bot.App.ApiClients.Scrapper.Errors.Chats;
using LinkTracker.Bot.App.ApiClients.Scrapper.Rest;
using LinkTracker.Bot.Core.HandlerTypes;
using LinkTracker.Bot.Core.HandlerTypes.Attributes;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace LinkTracker.Bot.App.Features.Bot.Start;

[BotCommand("start", "команда приветствия")]
public class StartMessageHandler(ITelegramBotClient bot, IScrapperClient scrapperClient) : IMessageHandler
{
    public async Task HandleAsync(Message msg, CancellationToken ct)
    {
        var result = await scrapperClient.AddTgChatAsync(msg.Chat.Id);
        var message = "Добро пожаловать! Используйте /help, чтобы посмотреть доступные команды";
        if (result.IsFailed)
        {
            var error = result.Errors.First();
            message = error switch
            {
                ChatAlreadyExists => "Добро пожаловать! Вы уже зарегистрированы. Используйте /help, чтобы посмотреть доступные команды",
                _ => "Произошла внутренняя ошибка"
            };
        }
        await bot.SendMessage(msg.Chat, message, cancellationToken: ct);
    }
}