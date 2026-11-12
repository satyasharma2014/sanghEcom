using Microsoft.EntityFrameworkCore;
using Payment.Infrastructure;
using Payment.Infrastructure.Persistence;
using Swashbuckle.AspNetCore;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddPaymentInfrastructure(builder.Configuration.GetConnectionString("PaymentDb")!);
builder.Services.AddHealthChecks();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    await db.Database.MigrateAsync();

}

app.UseHttpsRedirection();
app.MapControllers();
// Liveness: process zinda hai. Kubernetes isi se pod restart decide karta hai.
app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

// Readiness: traffic lene layak hai ya nahi.
app.MapHealthChecks("/health/ready");

// Migration-only mode: Kubernetes migration Job RUN_MIGRATIONS=true set karke chalati hai.
// Migration ke baad app start nahi hota (background workers bhi nahi chalte).
if (Environment.GetEnvironmentVariable("RUN_MIGRATIONS") == "true")
{
    using var migrationScope = app.Services.CreateScope();
    await migrationScope.ServiceProvider.GetRequiredService<PaymentDbContext>().Database.MigrateAsync();
    return;
}

app.Run();
