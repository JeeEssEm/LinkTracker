using System.Diagnostics.Metrics;

namespace LinkTracker.Scrapper.Metrics;

public class ApplicationMetrics
{
    private readonly Meter _meter;

    public Counter<long> GetList { get; }

    public ApplicationMetrics()
    {
        _meter = new Meter("LinkTracker.Scrapper.Metrics.ApplicationMetrics");
        GetList = _meter.CreateCounter<long>(
            name: "GetListCounter",
            unit: "Times",
            description: "How many times /get_list was called"
        );
    }
}