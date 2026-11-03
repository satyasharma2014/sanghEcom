//using BuildingBlocks.Contracts.Orders;
//using BuildingBlocks.Messaging;
//using BuildingBlocks.Outbox;
//using Microsoft.EntityFrameworkCore;
//using Notification.Api.Consumers;
//using Notification.Api.Infrastructure;

//var builder = WebApplication.CreateBuilder(args);

//builder.Services.AddControllers();
//builder.Services.AddOpenApi();
//// RabbitMQ settings + publisher
//builder.Services.AddSanghMessaging(builder.Configuration);

//// Notification database
//builder.Services.AddDbContext<NotificationDbContext>(o =>
//    o.UseNpgsql(builder.Configuration.GetConnectionString("NotificationDb")!));

//// Inbox (duplicate check) - consumer isi ko use karta hai
//builder.Services.AddScoped<InboxProcessor<NotificationDbContext>>();

//// Outbox dispatcher: outbox table se events RabbitMQ par bhejta hai
//builder.Services.AddHostedService(sp => new OutboxDispatcher<NotificationDbContext>(
//    sp.GetRequiredService<IServiceScopeFactory>(),
//    sp.GetRequiredService<RabbitMqOptions>(),
//    sp.GetRequiredService<ILogger<OutboxDispatcher<NotificationDbContext>>>()));

//// Consumer: order.placed sunkar email (mock) log karta hai
//builder.Services.AddHostedService<OrderPlacedNotificationConsumer>();

//var app = builder.Build();

//if (app.Environment.IsDevelopment())
//{
//    app.MapOpenApi();
//}

//app.MapControllers();

//app.MapGet("/health", () => Results.Ok(new { status = "Healthy", service = "notification-api" }));

//// ---------- TEST ENDPOINT (sirf development ke liye) ----------
//// Ye event ko sirf OUTBOX table mein save karta hai. RabbitMQ par dispatcher bhejta hai.
//app.MapPost("/dev/outbox-order-placed", async (NotificationDbContext db, CancellationToken ct) =>
//{
//    var evt = new OrderPlacedEvent(
//        Guid.NewGuid(),
//        "outbox@example.com",
//        1500m,
//        DateTime.UtcNow);

//    db.Set<OutboxMessage>().Add(OutboxMessage.Create(OrderRoutingKeys.OrderPlaced, evt));
//    await db.SaveChangesAsync(ct);

//    return Results.Accepted(value: evt);
//}).WithTags("Dev");

//app.Run();

using BuildingBlocks.Contracts.Orders;
using BuildingBlocks.Messaging;
using BuildingBlocks.Outbox;
using Microsoft.EntityFrameworkCore;
using Notification.Api.Consumers;
using Notification.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// RabbitMQ settings aur publisher
builder.Services.AddSanghMessaging(builder.Configuration);

// Notification database
builder.Services.AddDbContext<NotificationDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("NotificationDb")!));

// Background workers
builder.Services.AddHostedService<OutboxDispatcher<NotificationDbContext>>();
builder.Services.AddHostedService<OrderPlacedNotificationConsumer>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "Healthy", service = "notification-api" }));

// ================= DEV / TEST ENDPOINTS =================

// Outbox mein event save karta hai (202). Dispatcher baad mein RabbitMQ par bhejega.
app.MapPost("/dev/outbox-order-placed", async (NotificationDbContext db, CancellationToken ct) =>
{
    var evt = new OrderPlacedEvent(
        Guid.NewGuid(),
        "outbox@example.com",
        1500m,
        DateTime.UtcNow);

    db.Set<OutboxMessage>().Add(OutboxMessage.Create(OrderRoutingKeys.OrderPlaced, evt));
    await db.SaveChangesAsync(ct);

    return Results.Accepted(value: evt);
}).WithTags("Dev");

// Outbox ki latest 20 rows (ProcessedOnUtc, Attempts, LastError dikhte hain)
app.MapGet("/dev/outbox", async (NotificationDbContext db, CancellationToken ct) =>
{
    var rows = await db.Set<OutboxMessage>()
        .OrderByDescending(m => m.OccurredOnUtc)
        .Take(20)
        .Select(m => new
        {
            m.Id,
            m.RoutingKey,
            m.OccurredOnUtc,
            m.ProcessedOnUtc,
            m.Attempts,
            m.LastError
        })
        .ToListAsync(ct);

    return Results.Ok(rows);
}).WithTags("Dev");

// Notification records (mock email jo "bheje" gaye)
app.MapGet("/dev/notifications", async (NotificationDbContext db, CancellationToken ct) =>
{
    var rows = await db.NotificationRecords
        .OrderByDescending(n => n.SentAtUtc)
        .Take(20)
        .ToListAsync(ct);

    return Results.Ok(rows);
}).WithTags("Dev");

app.Run();
