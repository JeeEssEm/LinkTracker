using System.Diagnostics.Metrics;

namespace LinkTracker.Scrapper.Metrics;

public class ApplicationMetrics
{
    private readonly Meter _meter;

    public Gauge<long> MonitoredLinks { get; }
    public Counter<long> ApiRequests { get; }
    public Histogram<double> RequestDuration { get;}
    
    public ApplicationMetrics()
    {
        _meter = new Meter("LinkTracker.Scrapper.Metrics.ApplicationMetrics");
        
        MonitoredLinks = _meter.CreateGauge<long>("links_on_track");
        ApiRequests = _meter.CreateCounter<long>("api_requests");
        RequestDuration = _meter.CreateHistogram<double>("request_duration", "ms");
    }
}