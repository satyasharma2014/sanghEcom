//using System.Text;
//using System.Text.Json;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using RabbitMQ.Client;
//using RabbitMQ.Client.Events;

//namespace BuildingBlocks.Messaging;

///// <summary>
///// Reusable consumer. Subclass sirf HandleAsync likhti hai.
///// Fail hone par message DLQ mein jata hai.
///// </summary>
//public abstract class RabbitMqConsumerService<T>(
//    RabbitMqOptions options,
//    ILogger logger,
//    string routingKey,
//    string queueName) : BackgroundService
//{
//    protected abstract Task HandleAsync(T message, CancellationToken ct);
//    private static readonly JsonSerializerOptions JsonOptions = new()
//    {
//        PropertyNameCaseInsensitive = true
//    };

//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        while (!stoppingToken.IsCancellationRequested)
//        {
//            try
//            {
//                await RunAsync(stoppingToken);
//            }
//            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
//            {
//                break;
//            }
//            catch (Exception ex)
//            {
//                // RabbitMQ abhi up nahi: app crash nahi hogi, 5 second baad dobara
//                logger.LogError(ex, "Queue {Queue} connection fail. 5 second baad retry.", queueName);
//                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
//            }
//        }
//    }

//    private async Task RunAsync(CancellationToken ct)
//    {
//        var factory = new ConnectionFactory
//        {
//            HostName = options.Host,
//            Port = options.Port,
//            UserName = options.UserName,
//            Password = options.Password
//        };

//        await using var connection = await factory.CreateConnectionAsync(ct);
//        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);

//        // 1. Main exchange
//        await channel.ExchangeDeclareAsync(options.Exchange, ExchangeType.Topic, durable: true, cancellationToken: ct);

//        // 2. Dead letter exchange aur DLQ
//        var dlx = $"{queueName}.dlx";
//        var dlq = $"{queueName}.dlq";
//        await channel.ExchangeDeclareAsync(dlx, ExchangeType.Direct, durable: true, cancellationToken: ct);
//        await channel.QueueDeclareAsync(dlq, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
//        await channel.QueueBindAsync(dlq, dlx, queueName, cancellationToken: ct);

//        // 3. Main queue: reject hone wale message DLX par jayein
//        var args = new Dictionary<string, object?>
//        {
//            ["x-dead-letter-exchange"] = dlx,
//            ["x-dead-letter-routing-key"] = queueName
//        };
//        await channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false,
//            arguments: args, cancellationToken: ct);
//        await channel.QueueBindAsync(queueName, options.Exchange, routingKey, cancellationToken: ct);

//        // 4. Ek time par ek message (fair dispatch)
//        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false, cancellationToken: ct);

//        var consumer = new AsyncEventingBasicConsumer(channel);
//        consumer.ReceivedAsync += async (_, ea) =>
//        {
//            try
//            {
//                //var message = JsonSerializer.Deserialize<T>(ea.Body.Span)
//                //    ?? throw new JsonException("Body khali hai.");
//                var message = JsonSerializer.Deserialize<T>(ea.Body.Span, JsonOptions)
//    ?? throw new JsonException("Body khali hai.");

//                await HandleAsync(message, ct);
//                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: ct);
//            }
//            catch (Exception ex)
//            {
//                // Permanent fail: requeue nahi, seedha DLQ
//                logger.LogError(ex, "Message fail. MessageId: {MessageId}. DLQ mein bhej rahe hain.",
//                    ea.BasicProperties.MessageId);
//                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: ct);
//            }
//        };

//        await channel.BasicConsumeAsync(queueName, autoAck: false, consumer: consumer, cancellationToken: ct);
//        logger.LogInformation("Consumer chal raha hai. Queue: {Queue}", queueName);

//        // Jab tak channel chalta hai, ye task chalta rahega
//        var closed = new TaskCompletionSource();
//        channel.ChannelShutdownAsync += (_, _) => { closed.TrySetResult(); return Task.CompletedTask; };
//        await closed.Task.WaitAsync(ct);
//    }
//}
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BuildingBlocks.Messaging;

/// <summary>
/// Reusable consumer. Subclass sirf HandleAsync likhti hai.
/// Permanent fail par message DLQ mein jata hai.
/// </summary>
public abstract class RabbitMqConsumerService<T>(
    RabbitMqOptions options,
    ILogger logger,
    string routingKey,
    string queueName) : BackgroundService
{
    // Publisher PascalCase bhejta hai, consumer camelCase ya PascalCase dono padh sake
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    protected abstract Task HandleAsync(T message, CancellationToken ct);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Queue {Queue} connection fail. 5 second baad retry.", queueName);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task RunAsync(CancellationToken ct)
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

        var dlx = $"{queueName}.dlx";
        var dlq = $"{queueName}.dlq";
        await channel.ExchangeDeclareAsync(dlx, ExchangeType.Direct, durable: true, cancellationToken: ct);
        await channel.QueueDeclareAsync(dlq, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        await channel.QueueBindAsync(dlq, dlx, queueName, cancellationToken: ct);

        var args = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = dlx,
            ["x-dead-letter-routing-key"] = queueName
        };
        await channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false,
            arguments: args, cancellationToken: ct);
        await channel.QueueBindAsync(queueName, options.Exchange, routingKey, cancellationToken: ct);

        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false, cancellationToken: ct);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var message = JsonSerializer.Deserialize<T>(ea.Body.Span, JsonOptions)
                    ?? throw new JsonException("Body khali hai.");

                await HandleAsync(message, ct);
                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Message fail. MessageId: {MessageId}. DLQ mein bhej rahe hain.",
                    ea.BasicProperties.MessageId);
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: ct);
            }
        };

        await channel.BasicConsumeAsync(queueName, autoAck: false, consumer: consumer, cancellationToken: ct);
        logger.LogInformation("Consumer chal raha hai. Queue: {Queue}", queueName);

        var closed = new TaskCompletionSource();
        channel.ChannelShutdownAsync += (_, _) => { closed.TrySetResult(); return Task.CompletedTask; };
        await closed.Task.WaitAsync(ct);
    }
}
