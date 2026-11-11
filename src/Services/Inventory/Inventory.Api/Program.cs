using Inventory.Infrastructure;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddInventoryInfrastructure(builder.Configuration.GetConnectionString("InventoryDb")!);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    await db.Database.MigrateAsync();
}

app.UseHttpsRedirection();

//app.UseAuthorization();

app.MapControllers();

// Migration-only mode: Kubernetes migration Job RUN_MIGRATIONS=true set karke chalati hai.
// Migration ke baad app start nahi hota (background workers bhi nahi chalte).
if (Environment.GetEnvironmentVariable("RUN_MIGRATIONS") == "true")
{
    using var migrationScope = app.Services.CreateScope();
    await migrationScope.ServiceProvider.GetRequiredService<InventoryDbContext>().Database.MigrateAsync();
    return;
}

app.Run();
