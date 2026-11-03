//using BuildingBlocks.Messaging;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using RabbitMQ.Client;
//using System.Text;

//namespace BuildingBlocks.Outbox;

//public sealed class OutboxDispatcher<TContext>(
//    IServiceScopeFactory scopeFactory,
//    RabbitMqOptions options,
//    ILogger<OutboxDispatcher<TContext>> logger) : BackgroundService
//    where TContext : DbContext
//{
//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        while (!stoppingToken.IsCancellationRequested)
//        {
//            try
//            {
//                var sent = await DispatchBatchAsync(stoppingToken);
//                if (sent == 0)
//                {
//                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
//                }
//            }
//            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
//            {
//                break;
//            }
//            catch (Exception ex)
//            {
//                logger.LogError(ex, "Outbox dispatch error. 2 second baad retry.");
//                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
//            }
//        }
//    }

//    private async Task<int> DispatchBatchAsync(CancellationToken ct)
//    {
//        using var scope = scopeFactory.CreateScope();
//        var db = scope.ServiceProvider.GetRequiredService<TContext>();

//        var pending = await db.Set<OutboxMessage>()
//            .Where(m => m.ProcessedOnUtc == null)
//            .OrderBy(m => m.OccurredOnUtc)
//            .Take(20)
//            .ToListAsync(ct);

//        if (pending.Count == 0)
//        {
//            return 0;
//        }

//        var factory = new ConnectionFactory
//        {
//            HostName = options.Host,
//            Port = options.Port,
//            UserName = options.UserName,
//            Password = options.Password
//        };

//        await using var connection = await factory.CreateConnectionAsync(ct);
//        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
//        await channel.ExchangeDeclareAsync(options.Exchange, ExchangeType.Topic, durable: true, cancellationToken: ct);

//        foreach (var message in pending)
//        {
//            try
//            {
//                var props = new BasicProperties
//                {
//                    ContentType = "application/json",
//                    Persistent = true,
//                    MessageId = message.Id.ToString()   // inbox isi se duplicate pehchanega
//                };

//                await channel.BasicPublishAsync(
//                    options.Exchange,
//                    message.RoutingKey,
//                    mandatory: false,
//                    basicProperties: props,
//                    body: Encoding.UTF8.GetBytes(message.Payload),
//                    cancellationToken: ct);

//                message.MarkProcessed();
//            }
//            catch (Exception ex) when (ex is not OperationCanceledException)
//            {
//                message.RecordFailure(ex.Message);
//                logger.LogWarning(ex, "Outbox publish fail. Id: {Id}", message.Id);
//            }
//        }

//        await db.SaveChangesAsync(ct);
//        return pending.Count;
//    }
//}
using System.Text;
using BuildingBlocks.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace BuildingBlocks.Outbox;

/// <summary>
/// Background worker: outbox table ke pending events RabbitMQ par bhejta hai.
/// Broker down ho to events DB mein hi rehte hain, aur Attempts/LastError save hote hain.
/// </summary>
public sealed class OutboxDispatcher<TContext>(
    IServiceScopeFactory scopeFactory,
    RabbitMqOptions options,
    ILogger<OutboxDispatcher<TContext>> logger) : BackgroundService
    where TContext : DbContext
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Outbox dispatcher start hua.");

        while (!stoppingToken.IsCancellationRequested)
        {
            int published;
            try
            {
                published = await DispatchBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox dispatch mein error.");
                published = 0;
            }

            // Kuch publish nahi hua to 2 second wait, bar-bar DB aur broker par load na pade
            if (published == 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }

    /// <returns>Kitne messages successfully publish hue.</returns>
    private async Task<int> DispatchBatchAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TContext>();

        var pending = await db.Set<OutboxMessage>()
            .Where(m => m.ProcessedOnUtc == null)
            .OrderBy(m => m.OccurredOnUtc)
            .Take(20)
            .ToListAsync(ct);

        if (pending.Count == 0)
        {
            return 0;
        }

        logger.LogInformation("Outbox: {Count} pending message mile.", pending.Count);

        IConnection connection;
        IChannel channel;

        try
        {
            var factory = new ConnectionFactory
            {
                HostName = options.Host,
                Port = options.Port,
                UserName = options.UserName,
                Password = options.Password
            };

            connection = await factory.CreateConnectionAsync(ct);
            channel = await connection.CreateChannelAsync(cancellationToken: ct);
            await channel.ExchangeDeclareAsync(options.Exchange, ExchangeType.Topic, durable: true, cancellationToken: ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Broker down: har message par error save karo, taaki UI/API se dikhe
            foreach (var message in pending)
            {
                message.RecordFailure($"RabbitMQ connect fail: {ex.Message}");
            }

            await db.SaveChangesAsync(ct);
            logger.LogWarning(ex, "RabbitMQ connect fail. Messages DB mein safe hain.");
            return 0;
        }

        var success = 0;
        try
        {
            foreach (var message in pending)
            {
                try
                {
                    var props = new BasicProperties
                    {
                        ContentType = "application/json",
                        Persistent = true,
                        MessageId = message.Id.ToString()
                    };

                    await channel.BasicPublishAsync(
                        exchange: options.Exchange,
                        routingKey: message.RoutingKey,
                        mandatory: false,
                        basicProperties: props,
                        body: Encoding.UTF8.GetBytes(message.Payload),
                        cancellationToken: ct);

                    message.MarkProcessed();
                    success++;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    message.RecordFailure(ex.Message);
                    logger.LogWarning(ex, "Outbox publish fail. Id: {Id}", message.Id);
                }
            }

            await db.SaveChangesAsync(ct);
            logger.LogInformation("Outbox: {Success} message RabbitMQ par gaye.", success);
            return success;
        }
        finally
        {
            await channel.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
