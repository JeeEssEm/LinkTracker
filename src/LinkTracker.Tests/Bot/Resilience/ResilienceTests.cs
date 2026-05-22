using System.Diagnostics;
using System.Net;
using LinkTracker.Bot.App.ApiClients.Scrapper.Rest;
using LinkTracker.Scrapper.Configurations;
using LinkTracker.Scrapper.Extensions;
using LinkTracker.Scrapper.Infrastructure.Pipelines.ExternalApi.Github;
using LinkTracker.Scrapper.Infrastructure.Pipelines.ExternalApi.StackOverflow;
using LinkTracker.Tests.Shared;
using Microsoft.Extensions.DependencyInjection;
using Polly.CircuitBreaker;
using Polly.Timeout;
using Refit;

namespace LinkTracker.Tests.Bot.Resilience;

public class ResilienceTests
{
    private static IServiceProvider PrepareServiceProvider<TRefitClient>(MockHttpMessageHandler mockHandler, Action<ClientResilienceOptions> mutate) where TRefitClient : class
    {
        var options = new ClientResilienceOptions();
        mutate.Invoke(options);
        var sc = new ServiceCollection();
        sc
            .AddRefitClient<TRefitClient>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://some.com"))
            .ConfigurePrimaryHttpMessageHandler(() => mockHandler)
            .AddResilienceHandler(typeof(TRefitClient).ToString(), b => b.BuildPipeline(options));
        return sc.BuildServiceProvider();
    }

    [Fact]
    public async Task Test_ExceedTimeout()
    {
        var mockHandler = new MockHttpMessageHandler();
        mockHandler.SetSendLambda(async (r, ct) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
            return new HttpResponseMessage() { RequestMessage = r };
        });
        var sp = PrepareServiceProvider<IScrapperRefitClient>(mockHandler, o => o.TimeoutSeconds = 1);
        var client = sp.GetRequiredService<IScrapperRefitClient>();

        await Assert.ThrowsAsync<TimeoutRejectedException>(() => client.AddTgChatAsync(100));
    }

    [Fact]
    public async Task Test_RetryOnInternalError()
    {
        var mockHandler = new MockHttpMessageHandler();
        var count = 0;
        mockHandler.SetSendLambda((r, ct) =>
        {
            if (count < 2)
            {
                count++;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError) { RequestMessage = r });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { RequestMessage = r, Content = new StringContent("{\"items\": [], \"has_more\": false}") });
        });
        var sp = PrepareServiceProvider<IScrapperRefitClient>(mockHandler, o =>
        {
            o.RetryOptions.DelayMs = 0;
            o.RetryOptions.MaxAttempts = 3;
            o.RetryOptions.RetryableStatuses = [500];
        });
        var client = sp.GetRequiredService<IScrapperRefitClient>();
        var result = await client.AddTgChatAsync(1);
        Assert.True(result.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Test_DoNotRetryOn400()
    {
        var mockHandler = new MockHttpMessageHandler();
        var count = 0;
        mockHandler.SetSendLambda((r, ct) =>
        {
            count++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest) { RequestMessage = r });
        });
        var sp = PrepareServiceProvider<IScrapperRefitClient>(mockHandler, o =>
        {
            o.RetryOptions.DelayMs = 0;
            o.RetryOptions.MaxAttempts = 3;
            o.RetryOptions.RetryableStatuses = [];
        });
        var client = sp.GetRequiredService<IScrapperRefitClient>();
        var result = await client.AddTgChatAsync(1);
        Assert.False(result.IsSuccessStatusCode);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Test_RetryIntervalConstant()
    {
        const int delayMs = 100;
        const int toleranceMs = 40;

        var timestamps = new List<DateTimeOffset>();
        var mockHandler = new MockHttpMessageHandler();
        var count = 0;

        mockHandler.SetSendLambda((r, ct) =>
        {
            timestamps.Add(DateTimeOffset.UtcNow);
            if (count < 2)
            {
                count++;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError) { RequestMessage = r });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { RequestMessage = r, Content = new StringContent("{\"items\": [], \"has_more\": false}") });
        });
        var sp = PrepareServiceProvider<IScrapperRefitClient>(mockHandler, o =>
        {
            o.RetryOptions.DelayMs = delayMs;
            o.RetryOptions.MaxAttempts = 3;
            o.RetryOptions.RetryableStatuses = [500];
        });
        var client = sp.GetRequiredService<IScrapperRefitClient>();
        await client.AddTgChatAsync(1);

        Assert.Equal(3, timestamps.Count);

        var interval1 = (timestamps[1] - timestamps[0]).TotalMilliseconds;
        var interval2 = (timestamps[2] - timestamps[1]).TotalMilliseconds;

        Assert.InRange(interval1, delayMs - toleranceMs, delayMs + toleranceMs);
        Assert.InRange(interval2, delayMs - toleranceMs, delayMs + toleranceMs);
        Assert.True(Math.Abs(interval1 - interval2) < toleranceMs);
    }

    [Fact]
    public async Task CircuitBreaker_OpensAfterFailures()
    {
        var mockHandler = new MockHttpMessageHandler();
        mockHandler.SetSendLambda((r, _) => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.InternalServerError) { RequestMessage = r })
        );

        var sp = PrepareServiceProvider<IScrapperRefitClient>(mockHandler, o =>
        {
            o.CircuitBreakerOptions.MinimumRequiredCalls = 3;
            o.CircuitBreakerOptions.FailureRateThreshold = 1;
            o.CircuitBreakerOptions.WaitDurationInOpenStateSeconds = 10;
            o.RetryOptions.MaxAttempts = 0;
        });
        var client = sp.GetRequiredService<IScrapperRefitClient>();

        for (var i = 0; i < 3; i++)
        {
            await client.AddTgChatAsync(1);
        }

        var sw = Stopwatch.StartNew();
        await Assert.ThrowsAsync<BrokenCircuitException>(() =>
            client.ListLinksAsync(10));
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < 100);
    }

    [Fact]
    public async Task CircuitBreaker_HalfOpen_ClosesOnSuccess()
    {
        var mockHandler = new MockHttpMessageHandler();
        var shouldFail = true;

        mockHandler.SetSendLambda((r, ct) =>
        {
            var status = shouldFail
                ? HttpStatusCode.InternalServerError
                : HttpStatusCode.OK;
            return Task.FromResult(new HttpResponseMessage(status) { RequestMessage = r, Content = new StringContent("[]") });
        });

        var sp = PrepareServiceProvider<IScrapperRefitClient>(mockHandler, o =>
        {
            o.CircuitBreakerOptions.MinimumRequiredCalls = 3;
            o.CircuitBreakerOptions.FailureRateThreshold = 1;
            o.CircuitBreakerOptions.WaitDurationInOpenStateSeconds = 1;
            o.RetryOptions.MaxAttempts = 0;
        });
        var client = sp.GetRequiredService<IScrapperRefitClient>();

        for (var i = 0; i < 3; i++)
        {
            await client.ListLinksAsync(10);
        }

        await Assert.ThrowsAsync<BrokenCircuitException>(() =>
            client.AddTgChatAsync(10));

        await Task.Delay(TimeSpan.FromSeconds(1.1));

        shouldFail = false;

        var result = await client.AddTgChatAsync(10);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task CircuitBreaker_HalfOpen_OpensOnFailure()
    {
        var mockHandler = new MockHttpMessageHandler();
        mockHandler.SetSendLambda((r, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError) { RequestMessage = r }));

        var sp = PrepareServiceProvider<IScrapperRefitClient>(mockHandler, o =>
        {
            o.CircuitBreakerOptions.MinimumRequiredCalls = 3;
            o.CircuitBreakerOptions.FailureRateThreshold = 1;
            o.CircuitBreakerOptions.WaitDurationInOpenStateSeconds = 1;
            o.RetryOptions.MaxAttempts = 0;
        });
        var client = sp.GetRequiredService<IScrapperRefitClient>();

        for (var i = 0; i < 3; i++)
        {
            await client.AddTgChatAsync(i);
        }

        await Assert.ThrowsAsync<BrokenCircuitException>(() =>
            client.ListLinksAsync(4)); // CB: opened

        await Task.Delay(TimeSpan.FromSeconds(1.1)); // CB: half open

        await client.AddTgChatAsync(10);
        await Assert.ThrowsAsync<BrokenCircuitException>(() => client.AddTgChatAsync(1)); // CB: opened
    }
}