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
using Orders.Application.Orders;
using Orders.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddOrdersInfrastructure(builder.Configuration);
builder.Services.AddScoped<OrderService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", service = "orders-api" }));

app.Run();
