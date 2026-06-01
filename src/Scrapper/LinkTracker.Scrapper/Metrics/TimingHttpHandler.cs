using System.Diagnostics;

namespace LinkTracker.Scrapper.Metrics;

public class TimingHttpHandler(ApplicationMetrics metrics) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var resp = await base.SendAsync(request, cancellationToken);
        sw.Stop();
        metrics.RequestDuration.Record(
            sw.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("scope", RequestDurationScopes.ExternalSource),
            new KeyValuePair<string, object?>("scope_type", request.RequestUri?.Host ?? "unknown domain")
            );
        return resp;
    }
}