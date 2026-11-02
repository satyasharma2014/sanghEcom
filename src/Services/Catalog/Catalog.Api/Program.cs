using Catalog.Application.Products;
using BuildingBlocks.Common.Results;
using Catalog.Infrastructure;
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
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();

}

app.UseHttpsRedirection();

//app.UseAuthorization();

app.MapControllers();

app.Run();
