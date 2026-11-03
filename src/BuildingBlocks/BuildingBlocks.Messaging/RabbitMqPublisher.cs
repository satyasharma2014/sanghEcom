using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace BuildingBlocks.Messaging;

/// <summary>
/// Event ko exchange par publish karta hai. Orders service ise use karegi (Step 11).
/// </summary>
public sealed class RabbitMqPublisher(RabbitMqOptions options)
{
    public async Task PublishAsync<T>(string routingKey, T payload, CancellationToken ct = default)
    {
        var factory = new ConnectionFactory
        {
            HostName = options.Host,
            Port = options.Port,
            UserName = options.UserName,
            Password = options.Password
        };

        await using var connection = await factory.CreateConnectionAsync(ct);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);

        await channel.ExchangeDeclareAsync(options.Exchange, ExchangeType.Topic, durable: true, cancellationToken: ct);

        var props = new BasicProperties
        {
            ContentType = "application/json",
            Persistent = true,               // disk par save
            MessageId = Guid.NewGuid().ToString()
        };

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));

        await channel.BasicPublishAsync(
            exchange: options.Exchange,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: ct);
    }
}
