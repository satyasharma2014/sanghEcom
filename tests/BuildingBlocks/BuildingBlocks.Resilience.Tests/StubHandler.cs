namespace BuildingBlocks.Resilience.Tests;

internal sealed class StubHandler(Func<int, CancellationToken, Task<HttpResponseMessage>> behaviour)
    : HttpMessageHandler
{
    public int Calls { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Calls++;
        return await behaviour(Calls, cancellationToken);
    }
}
