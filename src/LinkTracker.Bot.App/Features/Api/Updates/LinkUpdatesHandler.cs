using FluentResults;
using Google.Rpc;
using LinkTracker.Bot.App.Features.Api.Shared.Errors;
using LinkTracker.Contracts.REST;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace LinkTracker.Bot.App.Features.Api.Updates;

public class LinkUpdatesHandler(ITelegramBotClient botClient, ILogger<LinkUpdatesHandler> logger)
{
    public async Task<Result> HandleAsync(LinkUpdatesRequest request)
    {
        if (request.TgChatIds is null || request.TgChatIds.Count == 0)
        {
            return Result.Fail(new IncorrectRequestParametersError("No chat IDs provided"));
        }

        var message = $"🔔 Обновление ссылки:\n{request.Url}\n\n{request.Description}";

        var tasks = request.TgChatIds
            .Select(async chatId =>
            {
                try
                {
                    await botClient.SendMessage(chatId, message);
                    return (Success: true, ChatId: chatId);
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Cannot send update to user with chat_id={id}", chatId);
                    return (Success: false, ChatId: chatId);
                }
            });

        var results = await Task.WhenAll(tasks);
        var failedToNotify = results
            .Where(t => !t.Success)
            .Select(t => t.ChatId)
            .ToList();

        if (failedToNotify.Count > 0)
        {
            return Result.Fail(new FailedToNotifyUsersError(failedToNotify));
        }

        return Result.Ok();
    }
}