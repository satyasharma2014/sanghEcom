namespace Orders.Application.Abstractions;

public interface IOutboxWriter
{
    void Add<T>(string routingKey, T payload);
}
