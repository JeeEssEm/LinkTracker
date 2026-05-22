using Confluent.Kafka;
using LinkTracker.Contracts.Broker;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Notifications;
using LinkTracker.Scrapper.Infrastructure.Pipelines.NotificationStrategies;

namespace LinkTracker.Scrapper.Extensions;

public static class NotificationStrategyExtensions
{
    public static async Task PrepareAsyncNotificationStrategy(this IServiceCollection services, TopicConfiguration topicConfig, NotificationStrategyOptions notificationStrategyOptions)
    {
        notificationStrategyOptions.ValidateKafkaStrategy();

        await KafkaBrokerManager.EnsureTopicExistsAsync(topicConfig, notificationStrategyOptions.Topic!, notificationStrategyOptions.BootstrapServers!);

        services.AddSingleton<IProducer<Null, string>>(_ =>
        {
            var config = new ProducerConfig
            {
                BootstrapServers = notificationStrategyOptions.BootstrapServers,
                Acks = Acks.Leader,
                BrokerAddressFamily = BrokerAddressFamily.V4
            };
            return new ProducerBuilder<Null, string>(config).Build();
        });
    }
}