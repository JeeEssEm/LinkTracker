using System.Net;
using System.Text;
using FluentResults;
using LinkTracker.Bot.App.ApiClients;
using LinkTracker.Bot.App.ApiClients.Scrapper;
using LinkTracker.Bot.App.ApiClients.Scrapper.Rest;
using LinkTracker.Bot.Core.HandlerTypes;
using LinkTracker.Bot.Core.HandlerTypes.Attributes;
using LinkTracker.Scrapper.Contracts.Dtos.Links;
using LinkTracker.Scrapper.Contracts.Errors;
using LinkTracker.Scrapper.Errors.Chats;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace LinkTracker.Bot.App.Features.Bot.ListLinks;

[BotCommand("links", "Выводит список доступных ссылок")]
public class GetLinksCommand(ITelegramBotClient botClient, IScrapperClient scrapperClient) : IMessageHandler
{
    public async Task HandleAsync(Message msg, CancellationToken ct)
    {
        var result = await scrapperClient.ListLinksAsync(msg.Chat.Id);
        if (result.IsFailed)
        {
            var error = result.Errors.First();
            var message = error switch
            {
                ChatNotFound => "Вы не зарегистрированы! Напишите /start",
                IncorrectRequestParameters => "Произошла ошибка при отправке запроса. Повторите попытку",
                _ => throw new InvalidOperationException()
            };
            await botClient.SendMessage(msg.Chat.Id, message, cancellationToken: ct);
            return;
        }

        var filter = ExtractFilterTag(msg.Text);
        if (filter.IsFailed)
        {
            await botClient.SendMessage(msg.Chat.Id, "Некорректный формат. Правильный формат: /links {тег}", cancellationToken: ct);
            return;
        }

        await botClient.SendMessage(msg.Chat.Id, BuildMessage(result.Value?.Links.ToList() ?? [], filter.Value), cancellationToken: ct);
    }

    private string BuildMessage(List<LinkResponse> links, string? tagFilter)
    {
        if (links.Count == 0)
        {
            return "У вас нет ссылок для отслеживания. Добавить ссылку /track";
        }

        var filtered = tagFilter != null
            ? links.Where(l => l.Tags.Contains(tagFilter)).ToList()
            : links;

        if (filtered.Count == 0)
        {
            return $"Ссылок с тегом \"{tagFilter}\" не найдено.";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"📋 Отслеживаемые ссылки ({filtered.Count}):");

        for (var i = 0; i < filtered.Count; i++)
        {
            var link = filtered[i];
            sb.Append($"{i + 1}. {link.Url}");
            if (link.Tags.Count > 0)
            {
                sb.Append($" [{string.Join(", ", link.Tags)}]");
            }
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    private Result<string> ExtractFilterTag(string message)
    {
        var parts = message.Split(" ");
        switch (parts.Length)
        {
            case 1:
                return Result.Ok();
            case 2:
                return Result.Ok(parts[1]);
            default:
                return Result.Fail("Некорректный формат ввода");
        }
    }
}