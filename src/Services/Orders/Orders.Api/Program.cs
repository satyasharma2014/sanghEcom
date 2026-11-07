//using Orders.Application.Orders;
//using Orders.Infrastructure;

//var builder = WebApplication.CreateBuilder(args);

//builder.Services.AddControllers();
//builder.Services.AddOpenApi();
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();
//builder.Services.AddOrdersInfrastructure(builder.Configuration.GetConnectionString("OrdersDb")!);
//builder.Services.AddScoped<OrderService>();

//var app = builder.Build();

//if (app.Environment.IsDevelopment())
//{
//    app.MapOpenApi();
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

//app.MapControllers();
//app.MapGet("/health", () => Results.Ok(new { status = "Healthy", service = "orders-api" }));

//app.Run();
////using Microsoft.EntityFrameworkCore;
////using Orders.Application.Orders;
////using Orders.Infrastructure;
////using Orders.Infrastructure.Persistence;
////using BuildingBlocks.Messaging;
////using BuildingBlocks.Outbox;


////var builder = WebApplication.CreateBuilder(args);

////builder.Services.AddControllers();
////builder.Services.AddOpenApi();
////builder.Services.AddOrdersInfrastructure(builder.Configuration);
////builder.Services.AddScoped<OrderService>();

////builder.Services.AddSanghMessaging(builder.Configuration);

////builder.Services.AddHostedService(sp => new OutboxDispatcher<OrdersDbContext>(
////    sp.GetRequiredService<IServiceScopeFactory>(),
////    sp.GetRequiredService<RabbitMqOptions>(),
////    sp.GetRequiredService<ILogger<OutboxDispatcher<OrdersDbContext>>>()));

////var app = builder.Build();

////if (app.Environment.IsDevelopment())
////{
////    app.MapOpenApi();
////    using var scope = app.Services.CreateScope();
////    var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
////    await db.Database.MigrateAsync();
////}

////app.MapControllers();
////app.MapGet("/health", () => Results.Ok(new { status = "Healthy", service = "orders-api" }));

////app.Run();
///
using BuildingBlocks.Messaging;
using BuildingBlocks.Outbox;
using Orders.Application.Orders;
using Orders.Infrastructure;
using Orders.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Orders DB, repositories, HTTP clients (catalog, inventory, payment), recovery worker
builder.Services.AddOrdersInfrastructure(builder.Configuration);
builder.Services.AddScoped<OrderService>();

// RabbitMQ settings aur publisher
builder.Services.AddSanghMessaging(builder.Configuration);

// Outbox dispatcher: orders_db ke outbox_messages ko RabbitMQ par bhejta hai
builder.Services.AddHostedService(sp => new OutboxDispatcher<OrdersDbContext>(
    sp.GetRequiredService<IServiceScopeFactory>(),
    sp.GetRequiredService<RabbitMqOptions>(),
    sp.GetRequiredService<ILogger<OutboxDispatcher<OrdersDbContext>>>()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", service = "orders-api" }));

app.Run();
