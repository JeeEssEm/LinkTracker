namespace LinkTracker.Bot.App.Configurations;

public class ResilienceOptions
{
    public const string Position = nameof(ResilienceOptions);
    public ClientResilienceOptions TelegramClientOptions { get; set; } = new();
    public ClientResilienceOptions ScrapperClientOptions { get; set; } = new();
    public RateLimitOptions RateLimitOptions { get; set; } = new();
}

public class ClientResilienceOptions
{
    public int TimeoutSeconds { get; set; } = 15;
    public CircuitBreakerOptions CircuitBreakerOptions { get; set; } = new();
    public RetryOptions RetryOptions { get; set; } = new();
}

public class RetryOptions
{
    public int MaxAttempts { get; set; } = 3;
    public int DelayMs { get; set; } = 500;
    public List<int> RetryableStatuses { get; set; } = [500, 502, 503];
}

public class CircuitBreakerOptions
{
    public int SlidingWindowSeconds { get; set; } = 10;
    public int MinimumRequiredCalls { get; set; } = 5;
    public float FailureRateThreshold { get; set; } = 1;
    public int WaitDurationInOpenStateSeconds { get; set; } = 1;
}

public class RateLimitOptions
{
    public int PermitLimit { get; set; } = 200;
    public int WindowSeconds { get; set; } = 60;
    public int QueueLimit { get; set; } = 10;
}
