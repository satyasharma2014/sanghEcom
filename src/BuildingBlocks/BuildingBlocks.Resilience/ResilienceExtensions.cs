//using System.Threading.RateLimiting;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Http.Resilience;
//using Polly;
//using Polly.RateLimiting;

//namespace BuildingBlocks.Resilience;

//public static class ResilienceExtensions
//{
//    /// <summary>
//    /// Kisi typed HttpClient par resilience lagata hai:
//    /// Bulkhead → Retry → Circuit Breaker → Timeout (per attempt).
//    /// </summary>
//    public static IHttpClientBuilder AddSanghResilience(
//        this IHttpClientBuilder builder,
//        ResilienceOptions options)
//    {
//        return (IHttpClientBuilder)builder.AddResilienceHandler($"{builder.Name}-resilience", pipeline =>
//        {
//            pipeline.AddConcurrencyLimiter(new ConcurrencyLimiterOptions
//            {
//                PermitLimit = options.BulkheadMaxParallel,
//                QueueLimit = options.BulkheadQueueLimit
//            });

//            pipeline.AddRetry(new HttpRetryStrategyOptions
//            {
//                MaxRetryAttempts = options.RetryCount,
//                BackoffType = DelayBackoffType.Exponential,
//                UseJitter = true,
//                Delay = TimeSpan.FromMilliseconds(options.RetryBaseDelayMs)
//            });

//            pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
//            {
//                FailureRatio = options.CircuitFailureRatio,
//                MinimumThroughput = options.CircuitMinimumThroughput,
//                SamplingDuration = TimeSpan.FromSeconds(options.CircuitSamplingSeconds),
//                BreakDuration = TimeSpan.FromSeconds(options.CircuitBreakSeconds)
//            });

//            pipeline.AddTimeout(TimeSpan.FromSeconds(options.AttemptTimeoutSeconds));
//        });
//    }
//}
////using System.Threading.RateLimiting;
////using Microsoft.Extensions.DependencyInjection;
////using Microsoft.Extensions.Http.Resilience;
////using Polly;
////using Polly.RateLimiting;

////namespace BuildingBlocks.Resilience;

////public static class ResilienceExtensions
////{
////    /// <summary>
////    /// Typed HttpClient par resilience pipeline lagata hai:
////    /// Bulkhead → Retry → Circuit Breaker → Timeout (per attempt).
////    /// </summary>
////    public static IHttpClientBuilder AddSanghResilience(
////        this IHttpClientBuilder builder,
////        ResilienceOptions options)
////    {
////        // Pipeline builder ka return value use nahi karna, sirf pipeline lagana hai.
////        // Isliye hum original "builder" return karte hain.
////        builder.AddResilienceHandler($"{builder.Name}-resilience", pipeline =>
////        {
////            pipeline.AddConcurrencyLimiter(new ConcurrencyLimiterOptions
////            {
////                PermitLimit = options.BulkheadMaxParallel,
////                QueueLimit = options.BulkheadQueueLimit
////            });

////            pipeline.AddRetry(new HttpRetryStrategyOptions
////            {
////                MaxRetryAttempts = options.RetryCount,
////                BackoffType = DelayBackoffType.Exponential,
////                UseJitter = true,
////                Delay = TimeSpan.FromMilliseconds(options.RetryBaseDelayMs)
////            });

////            pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
////            {
////                FailureRatio = options.CircuitFailureRatio,
////                MinimumThroughput = options.CircuitMinimumThroughput,
////                SamplingDuration = TimeSpan.FromSeconds(options.CircuitSamplingSeconds),
////                BreakDuration = TimeSpan.FromSeconds(options.CircuitBreakSeconds)
////            });

////            pipeline.AddTimeout(TimeSpan.FromSeconds(options.AttemptTimeoutSeconds));
////        });

////        return builder;
////    }
////}
using System.Threading.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.RateLimiting;

namespace BuildingBlocks.Resilience;

public static class ResilienceExtensions
{
    /// <summary>
    /// Typed HttpClient par resilience pipeline lagata hai:
    /// Bulkhead → Retry → Circuit Breaker → Timeout (per attempt).
    /// </summary>
    public static IHttpClientBuilder AddSanghResilience(
        this IHttpClientBuilder builder,
        ResilienceOptions options)
    {
        builder.AddResilienceHandler($"{builder.Name}-resilience", pipeline =>
        {
            pipeline.AddConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = options.BulkheadMaxParallel,
                QueueLimit = options.BulkheadQueueLimit
            });

            // Retry sirf tab, jab RetryCount 0 se zyada ho (Polly 0 retries allow nahi karta)
            if (options.RetryCount > 0)
            {
                pipeline.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = options.RetryCount,
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    Delay = TimeSpan.FromMilliseconds(options.RetryBaseDelayMs)
                });
            }

            pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
            {
                FailureRatio = options.CircuitFailureRatio,
                MinimumThroughput = options.CircuitMinimumThroughput,
                SamplingDuration = TimeSpan.FromSeconds(options.CircuitSamplingSeconds),
                BreakDuration = TimeSpan.FromSeconds(options.CircuitBreakSeconds)
            });

            pipeline.AddTimeout(TimeSpan.FromSeconds(options.AttemptTimeoutSeconds));
        });

        return builder;
    }
}
