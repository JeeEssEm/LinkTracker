using System.Text.Json;
using Confluent.Kafka;
using LinkTracker.Bot.App.Configurations;
using LinkTracker.Bot.App.Metrics;
using LinkTracker.Bot.Contracts.Rest;
using LinkTracker.Contracts.Broker;
using Microsoft.Extensions.Options;
using Telegram.Bot;

namespace LinkTracker.Bot.App.BackgroundServices.NotifierService;

public class ConsumerNotifierService(
    ConsumerConfig config,
    ITelegramBotClient botClient,
    ILogger<ConsumerNotifierService> logger,
    IOptions<KafkaConsumerOptions> options,
    IOptions<TopicConfiguration> topicConfig,
    ApplicationMetrics metrics) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await KafkaBrokerManager.EnsureTopicExistsAsync(topicConfig.Value, options.Value.Topic, options.Value.BootstrapServers);

        using var consumer = new ConsumerBuilder<Null, string>(config).Build();
        consumer.Subscribe(options.Value.Topic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var consumeResult = consumer.Consume(stoppingToken);
                var message = consumeResult.Message.Value;
                logger.LogDebug("Got new message from kafka: {msg}", message);
                var failedToNotify = new List<long>();

                var notification = JsonSerializer.Deserialize<LinkUpdateRequest>(message);
                foreach (var chatId in notification!.TgChatIds)
                {
                    try
                    {
                        await botClient.SendMessage(chatId, notification.Description, cancellationToken: stoppingToken);
                        metrics.SentNotificationCount.Add(1);
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "Failed to send notification to chatId={chatId}", chatId);
                        failedToNotify.Add(chatId);
                    }
                }

                consumer.Commit(consumeResult);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            consumer.Close();
        }
    }
}