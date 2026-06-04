using System.Diagnostics.Metrics;

namespace AiAgentService.Web.Metrics;

public class ApplicationMetrics
{
    private readonly Meter _meter;
    public Histogram<double> RequestDuration { get; }

    public ApplicationMetrics()
    {
        _meter = new Meter("AiAgentService.Web.Metrics.ApplicationMetrics");
        RequestDuration = _meter.CreateHistogram<double>("request_duration", "ms");
    }
}
