using System.Threading.Channels;
using FluentResults;
using Google.Rpc;
using LinkTracker.Bot.App.BackgroundServices.NotifierService;
using LinkTracker.Bot.Contracts.Errors;
using LinkTracker.Bot.Contracts.Rest;
using LinkTracker.Bot.Grpc.Links;
using LinkTracker.Scrapper.Contracts.Errors;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace LinkTracker.Bot.App.Features.Api.Updates;

public class LinkUpdatesHandler(ChannelWriter<NotifyEntry> entriesChannel)
{
    public async Task<Result> HandleAsync(LinkUpdateRequest request)
    {
        if (request.TgChatIds is null || request.TgChatIds.Count == 0)
        {
            return Result.Fail(new IncorrectRequestParameters("No chat IDs provided"));
        }

        var message = $"🔔 Обновление ссылки:\n{request.Url}\n\n{request.Description}";

        foreach (var chatId in request.TgChatIds)
        {
            await entriesChannel.WriteAsync(new NotifyEntry(message, chatId));
        }

        return Result.Ok();
    }
}