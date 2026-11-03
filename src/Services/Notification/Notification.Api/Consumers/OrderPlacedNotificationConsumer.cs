//using BuildingBlocks.Contracts.Orders;
//using BuildingBlocks.Messaging;

//namespace Notification.Api.Consumers;

//public sealed class OrderPlacedNotificationConsumer(
//    RabbitMqOptions options,
//    ILogger<OrderPlacedNotificationConsumer> logger)
//    : RabbitMqConsumerService<OrderPlacedEvent>(
//        options,
//        logger,
//        routingKey: OrderRoutingKeys.OrderPlaced,
//        queueName: "notification.order-placed")
//{
//    protected override Task HandleAsync(OrderPlacedEvent message, CancellationToken ct)
//    {
//        // Real email yahan lagegi. Abhi sirf log.
//        logger.LogInformation(
//            "[MOCK EMAIL] To: {Email} | OrderId: {OrderId} | Amount: {Amount}",
//            message.CustomerEmail, message.OrderId, message.TotalAmount);

//        return Task.CompletedTask;
//    }
//}
//using BuildingBlocks.Contracts.Orders;
//using BuildingBlocks.Messaging;
//using BuildingBlocks.Outbox;
//using Notification.Api.Domain;
//using Notification.Api.Infrastructure;

//namespace Notification.Api.Consumers;

//public sealed class OrderPlacedNotificationConsumer(
//    RabbitMqOptions options,
//    ILogger<OrderPlacedNotificationConsumer> logger,
//    IServiceScopeFactory scopeFactory)
//    : RabbitMqConsumerService<OrderPlacedEvent>(
//        options,
//        logger,
//        routingKey: OrderRoutingKeys.OrderPlaced,
//        queueName: "notification.order-placed")
//{
//    protected override async Task HandleAsync(OrderPlacedEvent message, CancellationToken ct)
//    {
//        using var scope = scopeFactory.CreateScope();
//        var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
//        var inbox = new InboxProcessor<NotificationDbContext>(db);

//        var processed = await inbox.ProcessOnceAsync(
//            messageId: message.OrderId.ToString(),
//            handler: nameof(OrderPlacedNotificationConsumer),
//            work: _ =>
//            {
//                db.NotificationRecords.Add(new NotificationRecord(
//                    message.OrderId, message.CustomerEmail, "Order placed"));

//                logger.LogInformation("[MOCK EMAIL] To: {Email} | OrderId: {OrderId}",
//                    message.CustomerEmail, message.OrderId);
//                return Task.CompletedTask;
//            },
//            ct);

//        if (!processed)
//        {
//            logger.LogInformation("Duplicate skip. OrderId: {OrderId}", message.OrderId);
//        }
//    }
//}
using BuildingBlocks.Contracts.Orders;
using BuildingBlocks.Messaging;
using BuildingBlocks.Outbox;
using Microsoft.Extensions.Logging;
using Notification.Api.Domain;
using Notification.Api.Infrastructure;

namespace Notification.Api.Consumers;

public sealed class OrderPlacedNotificationConsumer(
    RabbitMqOptions options,
    ILogger<OrderPlacedNotificationConsumer> logger,
    IServiceScopeFactory scopeFactory)
    : RabbitMqConsumerService<OrderPlacedEvent>(
        options,
        logger,
        routingKey: OrderRoutingKeys.OrderPlaced,
        queueName: "notification.order-placed")
{
    protected override async Task HandleAsync(OrderPlacedEvent message, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
        var inbox = new InboxProcessor<NotificationDbContext>(db);

        var processed = await inbox.ProcessOnceAsync(
            messageId: message.OrderId.ToString(),
            handler: nameof(OrderPlacedNotificationConsumer),
            work: _ =>
            {
                db.NotificationRecords.Add(new NotificationRecord(
                    message.OrderId, message.CustomerEmail, "Order placed"));

                logger.LogInformation("[MOCK EMAIL] To: {Email} | OrderId: {OrderId} | Amount: {Amount}",
                    message.CustomerEmail, message.OrderId, message.TotalAmount);

                return Task.CompletedTask;
            },
            ct);

        if (!processed)
        {
            logger.LogInformation("Duplicate skip. OrderId: {OrderId}", message.OrderId);
        }
    }
}
