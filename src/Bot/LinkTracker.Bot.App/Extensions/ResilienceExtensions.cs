using LinkTracker.Bot.App.Configurations;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace LinkTracker.Bot.App.Extensions;

public static class ResilienceExtensions
{
    public static void BuildPipeline(this ResiliencePipelineBuilder<HttpResponseMessage> builder, ClientResilienceOptions config)
    {
        builder
            .AddTimeout(TimeSpan.FromSeconds(config.TimeoutSeconds))
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>()
            {
                SamplingDuration = TimeSpan.FromSeconds(config.CircuitBreakerOptions.SlidingWindowSeconds),
                MinimumThroughput = config.CircuitBreakerOptions.MinimumRequiredCalls,
                FailureRatio = config.CircuitBreakerOptions.FailureRateThreshold,
                BreakDuration = TimeSpan.FromSeconds(config.CircuitBreakerOptions.WaitDurationInOpenStateSeconds),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .HandleResult(r => !r.IsSuccessStatusCode)
                    .Handle<HttpRequestException>()
            });

        if (config.RetryOptions.MaxAttempts > 0)
        {
            builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = config.RetryOptions.MaxAttempts,
                BackoffType = DelayBackoffType.Constant,
                Delay = TimeSpan.FromMilliseconds(config.RetryOptions.DelayMs),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .HandleResult(r => config.RetryOptions.RetryableStatuses.Contains((int)r.StatusCode))
                    .Handle<HttpRequestException>()
            });
        }
    }
}
