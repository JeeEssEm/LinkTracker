using System.Threading.Channels;
using Telegram.Bot;

namespace LinkTracker.Bot.App.BackgroundServices.NotifierService;

public class TelegramNotifierService(ITelegramBotClient botClient, ChannelReader<NotifyEntry> entriesChannel, ILogger<TelegramNotifierService> logger) : BackgroundService
{
    private const int TelegramMessagesPerSecondLimit = 30;

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var options = new ParallelOptions() { MaxDegreeOfParallelism = TelegramMessagesPerSecondLimit, CancellationToken = cancellationToken };
        await Parallel.ForEachAsync(
            entriesChannel.ReadAllAsync(cancellationToken),
            options,
            async (entry, token) =>
            {
                try
                {
                    await botClient.SendMessage(entry.ChatId, entry.Message, cancellationToken: token);
                }
                catch (Exception e)
                {
                    logger.LogError("Error occured while notifying user with id={chatId}: {exception}", entry.ChatId, e);
                }
            }
        );
    }
}
