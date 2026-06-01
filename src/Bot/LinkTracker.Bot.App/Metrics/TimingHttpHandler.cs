using System.Diagnostics;

namespace LinkTracker.Bot.App.Metrics;

public class TimingHttpHandler(ApplicationMetrics metrics) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var resp = await base.SendAsync(request, cancellationToken);
        sw.Stop();
        metrics.CommandDurationTotal.Record(
            sw.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>(CommandDurationLabels.Scope, CommandDurationScopes.ScrapperSyncApi),
            new KeyValuePair<string, object?>(CommandDurationLabels.ScopeType, request.Method.Method)
            );
        return resp;
    }
}