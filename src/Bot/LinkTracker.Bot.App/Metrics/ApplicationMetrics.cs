using System.Diagnostics.Metrics;

namespace LinkTracker.Bot.App.Metrics;

public class ApplicationMetrics
{
    private readonly Meter _meter;
    public Counter<long> CommandRequestsCount { get; }
    public Counter<long> SentNotificationCount { get; }
    public Histogram<double> CommandDurationTotal { get; }

    public ApplicationMetrics()
    {
        _meter = new Meter("LinkTracker.Bot.App.Metrics.BotMetrics");

        CommandRequestsCount = _meter.CreateCounter<long>("command_requests");
        SentNotificationCount = _meter.CreateCounter<long>("sent_notification");

        CommandDurationTotal = _meter.CreateHistogram<double>(
            name: "command_duration",
            unit: "ms"
            );
    }
}