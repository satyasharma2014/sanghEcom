//var builder = WebApplication.CreateBuilder(args);

//// Add services to the container.

//builder.Services.AddControllers();
//// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
//builder.Services.AddOpenApi();

//var app = builder.Build();

//// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.MapOpenApi();
//}

//app.UseHttpsRedirection();

//app.UseAuthorization();

//app.MapControllers();

//app.Run();
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Rate limit: har IP ke liye 100 requests per 60 second
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("per-ip", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromSeconds(60),
                QueueLimit = 0
            }));
});

// YARP: routes aur clusters appsettings.json se
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// Correlation ID: request ke header mein na ho to naya banega, response mein bhi jayega
app.Use(async (context, next) =>
{
    const string header = "X-Correlation-Id";
    var id = context.Request.Headers[header].FirstOrDefault();
    if (string.IsNullOrWhiteSpace(id))
    {
        id = Guid.NewGuid().ToString("N");
    }

    context.Request.Headers[header] = id;
    context.Response.Headers[header] = id;
    await next();
});

app.UseRateLimiter();

app.MapGet("/health", () => Results.Ok(new { status = "Healthy", service = "api-gateway" }));

app.MapReverseProxy().RequireRateLimiting("per-ip");

app.Run();
