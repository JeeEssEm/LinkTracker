using LinkTracker.Bot.Core.Dispatchers;
using LinkTracker.Bot.Core.Middlewares;
using LinkTracker.Bot.Core.Registries;
using LinkTracker.Bot.Core.StateMachine;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace LinkTracker.Bot.Core.BackgroundServices;

public class TelegramBotBackgroundService(ITelegramBotClient botClient, IServiceProvider serviceProvider, CommandRegistry commandRegistry, ILogger<TelegramBotBackgroundService> logger, UpdateDelegate pipeline) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await SetupBotCommands(stoppingToken);

        var receiverOptions = new ReceiverOptions() { AllowedUpdates = [UpdateType.Message, UpdateType.CallbackQuery] };

        botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandleErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: stoppingToken
        );
    }

    private async Task HandleUpdateAsync(ITelegramBotClient _, Update update, CancellationToken cancellationToken)
    {
        var (chatId, userId) = ExtractChatIdAndUserId(update);
        var logProperties = PrepareLogProperties(update, chatId, userId);

        using var loggerScope = logger.BeginScope(logProperties);
        using var scope = serviceProvider.CreateScope();

        var session = new UserSession { ChatId = chatId };
        var context = new UpdateContext
        {
            BotClient = botClient,
            CancellationToken = cancellationToken,
            Services = scope.ServiceProvider,
            Update = update,
            UserSession = session
        };
        var diSession = scope.ServiceProvider.GetRequiredService<UserSession>();
        diSession.ChatId = session.ChatId;

        try
        {
            await pipeline(context);
        }
        catch (Exception exc)
        {
            logger.LogError(exc, "Error while processing telegram update");
            await botClient.SendMessage(
                chatId,
                $"Произошла ошибка во время обработки запроса. Попробуйте повторить ваш запрос позже. Трейс: `{logProperties["Trace-Id"]}`",
                ParseMode.Markdown,
                cancellationToken: cancellationToken);
        }
    }

    private Task HandleErrorAsync(ITelegramBotClient _, Exception exception, CancellationToken ct)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        logger.LogError(exception, "Polling error occurred: {errorMsg}", errorMessage);
        return Task.CompletedTask;
    }

    private async Task SetupBotCommands(CancellationToken ct)
    {
        var botCommands = commandRegistry.GetBotCommands();
        if (botCommands.Count != 0)
        {
            await botClient.SetMyCommands(botCommands, cancellationToken: ct);
        }
    }

    private static Dictionary<string, object> PrepareLogProperties(Update update, long? chatId, long? userId)
    {
        var logProperties = new Dictionary<string, object> { ["UpdateId"] = update.Id, ["UpdateType"] = update.Type };
        if (chatId.HasValue)
        {
            logProperties["ChatId"] = chatId.Value;
        }

        if (userId.HasValue)
        {
            logProperties["UserId"] = userId.Value;
        }

        logProperties["Trace-Id"] = Guid.NewGuid();

        return logProperties;
    }

    private static (long ChatId, long UserId) ExtractChatIdAndUserId(Update update)
    {
        var chatId = update.Message?.Chat.Id ?? update.CallbackQuery?.Message?.Chat.Id;
        var userId = update.Message?.From?.Id ?? update.CallbackQuery?.From.Id;

        if (!chatId.HasValue || !userId.HasValue)
        {
            throw new ArgumentException("Receiver Options configured incorrecly. Cannot extract ChatId and UserId from Update");
        }

        return new(chatId.Value, userId.Value);
    }
}