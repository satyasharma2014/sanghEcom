using System.Net;
using BuildingBlocks.Resilience;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Polly.CircuitBreaker;
using Polly.Timeout;
using Xunit;

namespace BuildingBlocks.Resilience.Tests;

public sealed class ResilienceTests
{
    private const string Url = "http://inventory.local/api/v1/inventory/PHONE-001";

    private static ResilienceOptions Fast(int retry = 2, int timeoutSec = 3) => new()
    {
        RetryCount = retry,
        RetryBaseDelayMs = 10,
        AttemptTimeoutSeconds = timeoutSec,
        CircuitMinimumThroughput = 3,
        CircuitFailureRatio = 0.5,
        CircuitSamplingSeconds = 30,
        CircuitBreakSeconds = 30
    };

    private static (HttpClient client, StubHandler stub) Build(
        Func<int, CancellationToken, Task<HttpResponseMessage>> behaviour,
        ResilienceOptions options)
    {
        var stub = new StubHandler(behaviour);

        var services = new ServiceCollection();
        services.AddHttpClient("inventory")
            .ConfigurePrimaryHttpMessageHandler(() => stub)
            .AddSanghResilience(options);

        var factory = services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>();
        return (factory.CreateClient("inventory"), stub);
    }

    [Fact]
    public async Task Retry_TransientFailure_ThenSuccess_ReturnsOk()
    {
        var (client, stub) = Build(
            (call, _) => Task.FromResult(new HttpResponseMessage(
                call == 1 ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK)),
            Fast(retry: 2));

        var response = await client.GetAsync(Url);

        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal(2, stub.Calls);
    }

    [Fact]
    public async Task CircuitBreaker_OpensAfterRepeatedFailures()
    {
        var (client, stub) = Build(
            (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)),
            Fast(retry: 0));

        for (int i = 0; i < 3; i++)
        {
            await client.GetAsync(Url);
        }

        int callsBeforeOpen = stub.Calls;

        await Assert.ThrowsAsync<BrokenCircuitException>(() => client.GetAsync(Url));
        Assert.Equal(callsBeforeOpen, stub.Calls);
    }

    [Fact]
    public async Task Timeout_SlowAttempt_Throws()
    {
        var (client, _) = Build(
            async (_, ct) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
                return new HttpResponseMessage(HttpStatusCode.OK);
            },
            Fast(retry: 0, timeoutSec: 1));

        await Assert.ThrowsAsync<TimeoutRejectedException>(() => client.GetAsync(Url));
    }
}
