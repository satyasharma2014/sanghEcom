namespace BuildingBlocks.Resilience;

public sealed class ResilienceOptions
{
    public const string SectionName = "Resilience";

    // Retry
    public int RetryCount { get; init; } = 3;
    public int RetryBaseDelayMs { get; init; } = 200;

    // Timeout (har attempt par)
    public int AttemptTimeoutSeconds { get; init; } = 3;

    // Circuit Breaker
    public double CircuitFailureRatio { get; init; } = 0.5;
    public int CircuitMinimumThroughput { get; init; } = 5;
    public int CircuitSamplingSeconds { get; init; } = 30;
    public int CircuitBreakSeconds { get; init; } = 30;

    // Bulkhead (concurrency limit)
    public int BulkheadMaxParallel { get; init; } = 50;
    public int BulkheadQueueLimit { get; init; } = 20;
}
