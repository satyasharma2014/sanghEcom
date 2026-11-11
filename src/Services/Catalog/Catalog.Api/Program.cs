using Catalog.Application.Products;
using BuildingBlocks.Common.Results;
using Catalog.Infrastructure;
using Swashbuckle.AspNetCore;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;
using Microsoft.EntityFrameworkCore;
using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Infrastructure: DbContext, repository, unit of work
builder.Services.AddCatalogInfrastructure(
    builder.Configuration.GetConnectionString("CatalogDb")!);
// Application service
builder.Services.AddScoped<ProductService>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ProductQueryService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    await db.Database.MigrateAsync();

    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();

}

app.UseHttpsRedirection();

//app.UseAuthorization();

app.MapControllers();

// Migration-only mode: Kubernetes migration Job RUN_MIGRATIONS=true set karke chalati hai.
// Migration ke baad app start nahi hota (background workers bhi nahi chalte).
if (Environment.GetEnvironmentVariable("RUN_MIGRATIONS") == "true")
{
    using var migrationScope = app.Services.CreateScope();
    await migrationScope.ServiceProvider.GetRequiredService<CatalogDbContext>().Database.MigrateAsync();
    return;
}


app.Run();
